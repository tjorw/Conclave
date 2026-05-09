# ADR: Bokningskö och tilldelningsstrategier (R-BK01, R-BK02)

**Datum:** 2026-05-09  
**Status:** Godkänd

---

## Kontext

`RegisterForSessionHandler` skapar idag en `SessionRegistration` med status `Confirmed` direkt.
`RegistrationRuleService.ValidateSeatAvailability` returnerar alltid `true` — platskapaciteten
på `Session.MaxSeats` är alltså ett informationsfält utan koppling till bokningsflödet.

R-BK01 och R-BK02 kräver att:

- Vissa evenemang ska använda en köbaserad modell där intresseanmälningar samlas in
  och sedan fördelas av administratören.
- Tilldelning ska stödja tre strategier: **Först till kvarn**, **Lottning** och **Manuell**.
- Evenemang utan kö ska direkt bekräfta bokningen (nuvarande beteende), men nu med
  faktisk kapacitetsbegränsning.

---

## Beslut

### 1. `AllocationMode` på `Event`

Nytt enum på `Event`-aggregatet styr hur sessionsbokningar hanteras:

```
AllocationMode { DirectConfirmation, Queue }
```

- `DirectConfirmation` — bekräftar direkt, avvisar när `MaxSeats` är uppnått.  
- `Queue` — skapar en väntande bokning (`Pending`), kapacitet är ej begränsande vid anmälan.

Metod: `Event.ConfigureAllocationMode(AllocationMode mode, PersonId performedById)`.

Default är `DirectConfirmation` — inga befintliga evenemang påverkas av migrationen.

### 2. `Pending`-status på `SessionRegistration`

`SessionRegistrationStatus` utökas med `Pending`:

```
SessionRegistrationStatus { Pending, Confirmed, Cancelled }
```

Nya metoder på aggregatet:
- `SessionRegistration.Confirm()` — `Pending → Confirmed`, kastar om redan `Confirmed` eller `Cancelled`
- Befintligt `Cancel()` — fungerar nu även från `Pending`

Nya domain events:
- `SessionRegistrationQueued(registrationId, sessionId, personId, occurredAt)`
- `SessionRegistrationConfirmed(registrationId, sessionId, personId, occurredAt)`

### 3. Kapacitetskontroll i `RegisterForSessionHandler`

- `AllocationMode = DirectConfirmation`:  
  Hämtar antal bekräftade (`Confirmed`) registreringar för sessionen och jämför med `MaxSeats`.  
  Ger `DomainRuleViolationException` om fullt.
- `AllocationMode = Queue`:  
  Skapar `SessionRegistration` med `Pending`, ingen kapacitetskontroll.

### 4. `AllocateSessionRegistrationsCommand`

Nytt command (admin-operaton):

```
AllocateSessionRegistrationsCommand(
    Guid EventId,
    Guid SessionId,
    AllocationStrategy Strategy,         // FirstComeFirstServed | Lottery | Manual
    IReadOnlyList<Guid>? ManualIds = null // krävs vid Manual
)
```

Handler:
1. Hämtar evenemanget och verifierar admin-behörighet (EditionContextLoader + EnsureConventionAdmin).
2. Hämtar alla `Pending` registreringar för sessionen.
3. Räknar befintliga `Confirmed` registreringar (`alreadyConfirmed`).
4. Beräknar `available = session.MaxSeats - alreadyConfirmed`.
5. Väljer vilka som bekräftas baserat på strategi:
   - `FirstComeFirstServed`: sortera på `CreatedAt`, ta de `available` äldsta.
   - `Lottery`: slumpmässigt urval av `available` från `Pending`-listan.
   - `Manual`: bekräfta de angivna `ManualIds` (validera att de tillhör sessionen).
6. Anropar `registration.Confirm()` på valda, `registration.Cancel()` på resterande
   (gäller FCFS och Lottery; Manual lämnar övriga `Pending`).
7. Sparar via `ISessionRegistrationRepository.SaveAllAsync(registrations, ct)`.

### 5. `RegistrationRuleService`

`ValidateSeatAvailability` ersätts av en synkron check i `RegisterForSessionHandler`
baserat på en ny repository-metod:
`CountConfirmedBySessionIdAsync(SessionId, CancellationToken)`.

`ValidateSeatAvailability` på service-interfacet tas bort — logiken ägs nu av handlerarn.

---

## Motivering

**Varför `AllocationMode` på `Event` och inte på `Session`?**  
Roadmap-beskrivningen säger "arrangemanget äger reglerna". En session är en genomförandeinstans
av ett evenemang och ärver dess regler. Det är också enklare för admin att konfigurera läget
en gång per evenemang.

**Varför `Pending` som ny status på befintligt aggregat?**  
Alternativet (nytt `SessionBookingRequest`-aggregat) ger dubblering av identitets- och
biljettvalideringen och kräver att kön och registreringen synkroniseras. `Pending` som status
behåller ett enkelt objekt med tydlig livscykel.

**Varför skriva `Cancel()` på resterande Pending vid FCFS/Lottery?**  
Det ger omedelbar signal till intressenten utan att systemet behöver hålla en "väntande" kö
öppen på obestämd tid. `Manual`-strategin lämnar däremot övriga `Pending` för att admin ska
kunna köra ytterligare tilldelningsomgångar.

**Race condition vid DirectConfirmation:**  
Accepted risk vid konventionsskala. Sista skyddet är att repository-implementationen 
räknar om bekräftade platser precis innan save. Unik felkänning kastas om platsen tagits.

---

## Bounded contexts som påverkas

| BC | Vad ändras |
|---|---|
| **Event** (aggregat `Event`) | Ny property `AllocationMode`, ny metod `ConfigureAllocationMode()`, ny EF-kolumn `allocation_mode` |
| **Registration** (aggregat `SessionRegistration`) | Ny status `Pending`, metoder `Confirm()`, utökad `Cancel()`, nya domain events |
| **Registration** (service `RegistrationRuleService`) | `ValidateSeatAvailability` tas bort |
| **Registration** (handler `RegisterForSessionHandler`) | Grenar på `AllocationMode`, räknar kapacitet |
| **Registration** (nytt) | `AllocateSessionRegistrationsCommand` + handler |
| **Infrastructure** | Ny repository-metod `CountConfirmedBySessionIdAsync`, `SaveAllAsync`, migration |
| **API** | Ny endpoint `POST /api/events/{eventId}/sessions/{sessionId}/allocate` |
| **Admin-UI** | Sektion i event-detail för att köra tilldelning per session |

---

## Risker

| Risk | Sannolikhet | Åtgärd |
|---|---|---|
| Race condition vid DirectConfirmation | Låg (konventionsskala) | Accepterad; räkna om i repo precis innan save |
| Befintliga `Confirmed`-registreringar räknas vid allocation | Hög | Handlerar subtraherar redan bekräftade platser |
| Lottery-seed ger repeterbart utfall | Låg | Använd `Random.Shared` (ej seedat) |

---

## Acceptanskriterier

- [x] `Event.AllocationMode` kan sättas till `Queue` av admin; default är `DirectConfirmation`
- [x] `RegisterForSession` skapar `Pending` när `AllocationMode = Queue`
- [x] `RegisterForSession` avvisar när session är full vid `AllocationMode = DirectConfirmation`
- [x] `SessionRegistration.Confirm()` övergår status från `Pending` till `Confirmed`
- [x] `AllocateSessionRegistrations` med `FirstComeFirstServed` bekräftar äldsta `Pending` upp till `MaxSeats - alreadyConfirmed`; avbokar resten
- [x] `AllocateSessionRegistrations` med `Lottery` väljer slumpmässigt bland `Pending` upp till lediga platser; avbokar resten
- [x] `AllocateSessionRegistrations` med `Manual` bekräftar de angivna registreringarna; lämnar resterande `Pending`
- [x] En person kan inte ha mer än en aktiv (`Pending` eller `Confirmed`) registrering per session
- [x] Admin-UI visar antal `Pending` per session och knapp för att köra tilldelning (R-BK01c-ui + R-BK02c)
