# ADR: Laganmälningar (UC-TM001–UC-TM004)

**Datum:** 2026-05-08
**Status:** Beslutad

---

## Kontext

Evenemang med turneringsformat (t.ex. kortspel, brädspelstävlingar) behöver stödja laganmälningar. En captain anmäler ett lag till ett evenemang; arrangör eller admin bekräftar eller avbokar anmälan. I fas 1 behöver inte lagmedlemmarna namnges – det räcker med lagnamn och captain.

Det befintliga `RegistrationType`-fältet på `Event` (DropIn | PreRegistration | Combined) styr hur besökare *får tillgång* till eventet. Det är ortogonalt mot det nya `RegistrationMode` (Individual | Team) som styr *vem* som anmäler sig. Båda fälten ska finnas parallellt.

---

## Beslut

### Domänlager – Event BC

**Nytt enum:** `RegistrationMode { Individual, Team }` i `Event.Enums`.

**Nytt value object:** `TeamSize(int Min, int Max)` i `Event.ValueObjects`.

**Ny domänmetod på `Event`:**
```csharp
public void ConfigureTeamRegistration(RegistrationMode mode, int? minTeamSize, int? maxTeamSize)
```
Invarianter:
- Evenemanget får inte vara Cancelled.
- Om `mode = Team`: `minTeamSize ≥ 1` och `maxTeamSize ≥ minTeamSize`.
- Om `mode = Individual`: `TeamSize` sätts till null.

**Nya properties på `Event`:**
```csharp
public RegistrationMode RegistrationMode { get; private set; }  // default: Individual
public TeamSize? TeamSize { get; private set; }
```

**EF:** `registration_mode` lagras som `nvarchar(50)` (string-konvertering). `team_size_min` och `team_size_max` som nullable `int`. Ingen ny tabell – befintlig `events`-tabell utökas.

---

### Domänlager – Registration BC

#### `Team`-aggregat
```
Team
  TeamId                 (stark id-typ)
  EditionId              (Guid-referens till Edition)
  CaptainPersonId        (PersonId – den som skapade laget)
  Name                   (string, max 200)
  CreatedAt              (DateTimeOffset)
```
Domain event: `TeamCreated { TeamId, EditionId, CaptainPersonId, Name, OccurredAt }`

**Tabell:** `teams`

#### `TeamEventRegistration`-aggregat
```
TeamEventRegistration
  TeamEventRegistrationId (stark id-typ)
  TeamId                  (Guid-referens)
  EventId                 (Guid-referens)
  EditionId               (Guid-referens – denormaliserat för listqueries)
  Status                  (Pending | Confirmed | Cancelled)
  CreatedAt               (DateTimeOffset)
  UpdatedAt               (DateTimeOffset?)
```
Domänmetoder:
- `Confirm()` – kräver `Status = Pending`; sätter `Status = Confirmed`
- `Cancel(PersonId cancelledBy)` – kräver `Status ∈ {Pending, Confirmed}`; sätter `Status = Cancelled`

Domain events:
- `TeamEventRegistrationCreated { RegistrationId, TeamId, EventId, OccurredAt }`
- `TeamEventRegistrationConfirmed { RegistrationId, TeamId, EventId, OccurredAt }`
- `TeamEventRegistrationCancelled { RegistrationId, TeamId, EventId, CancelledByPersonId, OccurredAt }`

**Tabell:** `team_event_registrations`

---

### Applikationslager

| Command | Handler | Endpoint |
|---------|---------|----------|
| `ConfigureTeamRegistrationCommand(EventId, RegistrationMode, MinTeamSize?, MaxTeamSize?)` | `ConfigureTeamRegistrationHandler` | `PUT /api/events/{id}/registration-mode` |
| `RegisterTeamForEventCommand(EventId, EditionId, TeamName)` | `RegisterTeamForEventHandler` | `POST /api/events/{id}/team-registrations` |
| `ConfirmTeamRegistrationCommand(TeamEventRegistrationId)` | `ConfirmTeamRegistrationHandler` | `POST /api/team-registrations/{id}/confirm` |
| `CancelTeamRegistrationCommand(TeamEventRegistrationId)` | `CancelTeamRegistrationHandler` | `POST /api/team-registrations/{id}/cancel` |

| Query | Handler | Endpoint |
|-------|---------|----------|
| `ListTeamRegistrationsQuery(EventId)` | `ListTeamRegistrationsHandler` | `GET /api/events/{id}/team-registrations` |
| `GetTeamRegistrationQuery(TeamEventRegistrationId)` | `GetTeamRegistrationHandler` | `GET /api/team-registrations/{id}` |

**Nya repository-interfaces:**
- `ITeamRepository` (`AddAndSaveAsync`, `GetByIdAsync`, `SaveAsync`)
- `ITeamEventRegistrationRepository` (`AddAndSaveAsync`, `GetByIdAsync`, `ListByEventIdAsync`, `SaveAsync`)

**Behörighetsregler:**
- `ConfigureTeamRegistration` → `IsAdmin` (konventionsadministratör)
- `RegisterTeamForEvent` → autentiserad (captain = inloggad person)
- `ConfirmTeamRegistration` → `IsAdmin` (arrangör eller admin, kontrolleras i handler)
- `CancelTeamRegistration` → autentiserad (captain kan avboka eget lag; admin kan avboka alla)

**Affärsregler i handlers:**
- `RegisterTeamForEvent`: evenemanget måste ha `RegistrationMode = Team` och `Status = Published`. Personen får inte ha en aktiv (`Pending` eller `Confirmed`) anmälan för samma event.
- `ConfirmTeamRegistration`: utföraren är admin eller arrangör för eventet.
- `CancelTeamRegistration`: utföraren är captain (CaptainPersonId) eller admin.

---

### Infrastrukturlager

**EF-konfigurationer:**
- `TeamConfiguration` → tabell `teams`
- `TeamEventRegistrationConfiguration` → tabell `team_event_registrations`
- `EventConfiguration` utökas med `registration_mode`, `team_size_min`, `team_size_max`

**Index:**
- `IX_teams_edition_id`
- `IX_teams_captain_person_id`
- `IX_team_event_registrations_team_id`
- `IX_team_event_registrations_event_id`
- Sammansatt unikt: `UX_team_event_registrations_team_event` på `(team_id, event_id)` — ett lag kan bara ha en registrering per evenemang

**Migration:** `AddTeamRegistrations`

---

### API-lager

```
PUT  /api/events/{eventId}/registration-mode       → ConfigureTeamRegistrationCommand  [IsAdmin]
POST /api/events/{eventId}/team-registrations      → RegisterTeamForEventCommand       [Auth]
GET  /api/events/{eventId}/team-registrations      → ListTeamRegistrationsQuery        [IsAdmin]
GET  /api/team-registrations/{id}                  → GetTeamRegistrationQuery          [Auth]
POST /api/team-registrations/{id}/confirm          → ConfirmTeamRegistrationCommand    [Auth]
POST /api/team-registrations/{id}/cancel           → CancelTeamRegistrationCommand     [Auth]
```

---

### Frontend

**Admin-app:**
- Evenemangsdetalj: lägg till sektion för anmälningsläge (Individual / Team + lagstorlek). Visas bredvid befintlig RegistrationType-inställning.
- Nytt nav-område i evenemangsdetalj: "Laganmälningar" – lista med status, bekräfta/avboka-knappar.

**Publik app / Portal:** Utanför fas 1-scope (R-TM06). Registrerings-API:t är klart och kan nås, men dedikerat UI för captain definieras i separat ADR.

---

## Motivering

- Separata aggregat (`Team` + `TeamEventRegistration`) ger tydliga transaktionsgränser och stödjer framtida tillägg (t.ex. `Members[]`, `TeamSessionAssignment`).
- `EditionId` denormaliseras på `TeamEventRegistration` för att listqueries mot en upplaga inte ska behöva joina via `Team`.
- Unikt sammansatt index på `(team_id, event_id)` förhindrar dubblettanmälningar på DB-nivå som sista skydd.
- `RegistrationMode` är skilt från `RegistrationType` – koncepten är ortogonala och ska inte blandas.

---

## Bounded contexts som påverkas

| BC | Förändring |
|----|-----------|
| Event | Ny `RegistrationMode`-property + `TeamSize` value object + `ConfigureTeamRegistration()`-metod |
| Registration | Nya aggregat `Team` och `TeamEventRegistration`; nya repositories, commands, queries |
| Infrastructure | Två nya EF-konfigurationer, uppdaterad EventConfiguration, ny migration |
| API | Nytt `TeamRegistrationEndpoints`; `EventEndpoints` utökas med registration-mode endpoint |

---

## Risker

- **Övergång Individual → Team** när aktiva SessionRegistrations finns: UC:t anger inga regler om detta. Beslut: `ConfigureTeamRegistration` blockerar INTE befintliga SessionRegistrations – arrangören ansvarar för kommunikation. Ingen invariant i domänen.
- **Captain-check vid Cancel**: cross-aggregat-query behövs (hämta Team för att läsa `CaptainPersonId`). Handler laddar Team separat.
- **Ingen email-notis** för laganmälningar i fas 1. Läggs till separat.

---

## Acceptanskriterier

### UC-TM001
- [ ] `R-TM01` `Event.RegistrationMode` och `TeamSize` value object finns; `ConfigureTeamRegistration()` validerar och sätter; enhetstest täcker lyckligt flöde + invarianter
- [ ] Kommando, handler och endpoint implementerade; `IsAdmin` krävs
- [ ] RegistrationMode sparas korrekt i DB
- [ ] MinTeamSize < 1 → valideringsfel
- [ ] MaxTeamSize < MinTeamSize → valideringsfel
- [ ] Individual med angiven TeamSize → valideringsfel
- [ ] Admin-UI: dropdown Individual/Team + fält för lagstorlek visas conditionally

### UC-TM002
- [ ] `R-TM02` `Team`-aggregat med captain och namn; `TeamCreated`-event; enhetstest
- [ ] `R-TM03` `TeamEventRegistration`-aggregat, status `Pending` vid skapande; enhetstest
- [ ] Kommando, handler, validator och endpoint implementerade
- [ ] Evenemanget måste ha `RegistrationMode = Team` och `Status = Published` – annars 422
- [ ] En person kan inte ha två aktiva registreringar (Pending/Confirmed) per evenemang – annars 422
- [ ] `Team` och `TeamEventRegistration` skapas; `CaptainPersonId` = anmälande person

### UC-TM003
- [ ] `R-TM03` Domänmetod `Confirm()` med invariant att status är Pending; enhetstest
- [ ] Kommando, handler och endpoint implementerade
- [ ] Admin/arrangör kan bekräfta; annan person → 403
- [ ] Redan Confirmed/Cancelled → 422

### UC-TM004
- [ ] `R-TM03` Domänmetod `Cancel(cancelledByPersonId)` med invariant att status är Pending/Confirmed; enhetstest
- [ ] Kommando, handler och endpoint implementerade
- [ ] Captain kan avboka eget lag; admin kan avboka vilket lag som helst; annan person → 403
- [ ] Redan Cancelled → 422

### Queries
- [ ] `GET /api/events/{id}/team-registrations` returnerar lista med lag, status och captain-info
- [ ] Admin-UI: lista med laganmälningar per evenemang med bekräfta/avboka-knappar
