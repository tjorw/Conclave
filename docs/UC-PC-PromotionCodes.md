# UC-PC: Promotionkoder

Bounded context: **Registration**  
Prioritet: R19  
Status: `[ ]` Ej implementerad

---

## Bakgrund och syfte

Konventionsadministratörer behöver kunna dela ut fribiljetter och rabatter till utvalda personer – arrangörer, press, sponsorer, hedersdeltagare. I stället för att tilldela biljetter manuellt kan en administratör skapa en promotionkod som mottagaren löser in vid sin besöksregistrering.

En promotionkod kan antingen täcka hela biljettpriset (fri biljett) eller ge en procentuell eller fast rabatt.

---

## Domänmodell

### Nytt aggregat: `PromotionCode`

Lever i `Registration`-bounded context.

```
PromotionCode <<AggregateRoot>>
──────────────────────────────
id:               PromotionCodeId          (Guid, v7)
editionId:        EditionId
code:             string                   (unik per upplaga, versalokänslig vid inlösning)
description:      string                   (intern notering, visas ej för inlösaren)
discountType:     DiscountType             (Percentage | Fixed | Free)
discountValue:    decimal                  (0–100 för Percentage, ≥0 för Fixed, ignoreras för Free)
maxRedemptions:   int?                     (null = obegränsat)
redemptionCount:  int                      (ökas vid inlösning, private set)
validFrom:        DateTimeOffset?
validUntil:       DateTimeOffset?
allowedTicketTypeIds: IReadOnlyList<TicketTypeId>   (tom = alla biljetttyper)
isActive:         bool
createdAt:        DateTimeOffset
createdById:      PersonId

── Metoder ──
Redeem(personId, ticketTypeId, now) → PromotionCodeRedeemed
Deactivate(performedById)           → PromotionCodeDeactivated
```

### Ny entitet: `PromotionCodeRedemption`

Kopplar en inlösning till ett specifikt `Ticket`.

```
PromotionCodeRedemption <<Entity>>
──────────────────────────────────
id:             PromotionCodeRedemptionId
promotionCodeId: PromotionCodeId
ticketId:       TicketId
personId:       PersonId
redeemedAt:     DateTimeOffset
discountApplied: decimal              (det faktiska belopp som drogs av, snapshot)
```

### Ändring i `Ticket`

```diff
+ promotionCodeRedemptionId: PromotionCodeRedemptionId?   (null = inget kampanjpris)
+ finalPrice:               decimal                        (pris efter rabatt)
```

`finalPrice` beräknas av `RegistrationRuleService` och lagras som snapshot – ändrade kampanjkoder påverkar inte redan utfärdade biljetter.

### Value objects

```
DiscountType: Percentage | Fixed | Free
```

### Domain events

```
PromotionCodeCreated     { promotionCodeId, editionId, code, createdById, occurredAt }
PromotionCodeRedeemed    { promotionCodeId, ticketId, personId, discountApplied, occurredAt }
PromotionCodeDeactivated { promotionCodeId, performedById, occurredAt }
```

---

## Domänregler

| # | Regel |
|---|-------|
| DR1 | Koden måste vara aktiv (`isActive = true`) vid inlösning. |
| DR2 | `redemptionCount` får inte överskrida `maxRedemptions` (om satt). |
| DR3 | Aktuell tid (`now`) måste falla inom `validFrom`–`validUntil` (om satta). |
| DR4 | `ticketTypeId` måste finnas i `allowedTicketTypeIds` om listan är icke-tom. |
| DR5 | Samma person kan lösa in samma kod flera gånger (olika biljetter), om inga andra regler hindrar det. Konventionen väljer att begränsa via `maxRedemptions` om så önskas. |
| DR6 | `code` måste vara unik per `editionId` (enforced av unique index). |
| DR7 | `discountValue` för `Percentage` måste vara i intervallet 0–100. |
| DR8 | `finalPrice` kan aldrig bli negativ – golv vid 0. |
| DR9 | En deaktiverad kod (`isActive = false`) kan inte återaktiveras via domänmodellen. |

---

## Use cases

### UC-PC001 – Skapa promotionkod

**Aktör:** Konventionsadministratör  
**Förutsättning:** Upplagan existerar.

**Flöde:**
1. Administratören anger kod, beskrivning, rabattyp och -värde, valfria begränsningar (maxInlösningar, giltighetstid, biljetttyper).
2. Systemet validerar att koden är unik för upplagan.
3. `PromotionCode`-aggregat skapas. `PromotionCodeCreated` dispatches.
4. Systemet returnerar det nya `promotionCodeId`.

**Command:**
```csharp
sealed record CreatePromotionCodeCommand(
    Guid EditionId,
    string Code,
    string Description,
    string DiscountType,       // "Percentage" | "Fixed" | "Free"
    decimal DiscountValue,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<Guid> AllowedTicketTypeIds
) : IRequest<Guid>;
```

**Felfall:**
- Koden är inte unik för upplagan → `DomainRuleViolationException` ("Kampanjkoden finns redan för denna upplaga.")
- `DiscountValue` utanför giltigt intervall → valideringsfel
- `ValidFrom` > `ValidUntil` → valideringsfel

---

### UC-PC002 – Lista promotionkoder för en upplaga

**Aktör:** Konventionsadministratör  
**Förutsättning:** Upplagan existerar.

**Flöde:**
1. Systemet returnerar alla promotionkoder för upplagan, inklusive `redemptionCount` och `maxRedemptions`.

**Query:**
```csharp
sealed record ListPromotionCodesQuery(Guid EditionId) : IRequest<IReadOnlyList<PromotionCodeDto>>;
```

**PromotionCodeDto:**
```csharp
sealed record PromotionCodeDto(
    Guid Id,
    string Code,
    string Description,
    string DiscountType,
    decimal DiscountValue,
    int RedemptionCount,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<Guid> AllowedTicketTypeIds,
    bool IsActive
);
```

---

### UC-PC003 – Lösa in promotionkod

**Aktör:** Autentiserad besökare  
**Förutsättning:** Besökaren håller på att skapa eller har en `VisitorRegistration`. En `Ticket` i status `Reserved` är kopplad till registreringen.

**Flöde:**
1. Besökaren anger kampanjkoden i betalningssteget.
2. Systemet slår upp koden (versalokänslig matchning) för aktuell upplaga.
3. `RegistrationRuleService.ValidatePromotionCode(...)` kontrollerar domänreglerna DR1–DR4.
4. `promotionCode.Redeem(personId, ticketTypeId, now)` anropas. `redemptionCount` ökas.
5. `Ticket` uppdateras: `finalPrice` beräknas, `promotionCodeRedemptionId` sätts.
6. `PromotionCodeRedeemed` dispatches.
7. Om `finalPrice == 0`: biljetten transiteras direkt till `Paid` utan betalningsflöde (fri biljett).

**Command:**
```csharp
sealed record RedeemPromotionCodeCommand(
    Guid TicketId,
    string Code
) : IRequest;
```

**Felfall:**
- Koden hittas inte → `ResourceNotFoundException`
- Koden är inaktiv → `DomainRuleViolationException` ("Kampanjkoden är inte längre aktiv.")
- Max antal inlösningar nått → `DomainRuleViolationException` ("Kampanjkoden har nått sitt maximala antal inlösningar.")
- Utanför giltighetstid → `DomainRuleViolationException` ("Kampanjkoden är inte giltig just nu.")
- Biljetttyp ej tillåten → `DomainRuleViolationException` ("Kampanjkoden gäller inte för denna biljetttyp.")
- `Ticket` är inte i status `Reserved` → `DomainRuleViolationException`

---

### UC-PC004 – Deaktivera promotionkod

**Aktör:** Konventionsadministratör  
**Förutsättning:** Koden existerar och är aktiv.

**Flöde:**
1. Administratören deaktiverar koden.
2. `promotionCode.Deactivate(performedById)` anropas. `isActive` sätts till `false`.
3. `PromotionCodeDeactivated` dispatches.
4. Befintliga inlösningar och `Ticket`s påverkas inte.

**Command:**
```csharp
sealed record DeactivatePromotionCodeCommand(
    Guid PromotionCodeId
) : IRequest;
```

---

### UC-PC005 – Visa inlösningshistorik för en promotionkod

**Aktör:** Konventionsadministratör

**Flöde:**
1. Systemet returnerar alla `PromotionCodeRedemption`-poster för koden, med `personId`, `ticketId`, `redeemedAt` och `discountApplied`.

**Query:**
```csharp
sealed record GetPromotionCodeRedemptionsQuery(Guid PromotionCodeId)
    : IRequest<IReadOnlyList<PromotionCodeRedemptionDto>>;
```

---

## Infrastruktur

### Repository

```csharp
// Registration/Abstractions/IPromotionCodeRepository.cs
interface IPromotionCodeRepository
{
    Task<PromotionCode?> GetByIdAsync(PromotionCodeId id, CancellationToken ct = default);
    Task<PromotionCode?> GetByCodeAsync(EditionId editionId, string code, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCodeDto>> ListByEditionAsync(EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCodeRedemptionDto>> ListRedemptionsByCodeAsync(PromotionCodeId id, CancellationToken ct = default);
    void Add(PromotionCode promotionCode);
    Task SaveAsync(CancellationToken ct = default);
}
```

### EF Core-konfiguration

- Unik index på `(EditionId, Code)` – Code lagras i uppercase i databasen, matchning sker med `ToUpper()` i query.
- `allowedTicketTypeIds` serialiseras som JSON-kolumn (`[Guid]`).
- `PromotionCodeRedemption` konfigureras som owned entity collection på `PromotionCode`.

### `RegistrationRuleService` – tillägg

```csharp
void ValidatePromotionCode(
    PromotionCode code,
    TicketTypeId ticketTypeId,
    DateTimeOffset now);
```

Kastar `DomainRuleViolationException` med svenska felmeddelanden vid regelbrott (DR1–DR4).

---

## API-endpoints

Alla endpoints i `PromotionCodeEndpoints.cs`.

| Metod | URL | Auth | Use case |
|-------|-----|------|----------|
| `POST` | `/editions/{editionId}/promotion-codes` | Admin | UC-PC001 |
| `GET` | `/editions/{editionId}/promotion-codes` | Admin | UC-PC002 |
| `POST` | `/tickets/{ticketId}/redeem-promotion-code` | Auth | UC-PC003 |
| `DELETE` | `/promotion-codes/{promotionCodeId}` | Admin | UC-PC004 |
| `GET` | `/promotion-codes/{promotionCodeId}/redemptions` | Admin | UC-PC005 |

---

## Frontend – admin-app

### Ny sida: `PromotionCodesComponent`

Route: `/editions/:editionId/promotion-codes`  
Länk i edition-navigationen under "Biljetter".

**Layout:** page-header → action-bar → inline create-card → data-table

**Tabellkolumner:** Kod | Typ | Värde | Inlöst / Max | Giltig | Status | Åtgärder (Deaktivera)

**Create-formulär (inline):**
- Kod (text, required)
- Beskrivning (text)
- Rabattyp (select: Procentuell / Fast / Fri biljett)
- Värde (number, döljs för Fri biljett)
- Max inlösningar (number, optional)
- Giltig från / till (date, optional)
- Biljetttyper (multi-select, optional – tom = alla)

### Ändringar i besöksregistreringsflödet (publik app)

I betalningssteget:
- Textfält för kampanjkod + "Lös in"-knapp
- Vid lyckad inlösning: visa rabatt och nytt totalpris
- Vid fri biljett: dölj betalningssteg, visa bekräftelse direkt

---

## Migrering

En ny EF Core-migration behövs:

```
AddPromotionCodesTables
```

Tabeller:
- `PromotionCodes` (aggregatrot)
- `PromotionCodeRedemptions` (owned entity)

Kolumn på `Tickets`:
- `PromotionCodeRedemptionId` (nullable FK)
- `FinalPrice` (decimal, nullable – null innebär att `TicketType.Price` gäller)

---

## Acceptanskriterier

### UC-PC001
- [ ] Administratör kan skapa en kampanjkod med alla fält.
- [ ] Duplikat kod för samma upplaga ger 422 med felkod `promotion_code_already_exists`.
- [ ] Fri biljett-kod skapar kod med `DiscountType = Free`.

### UC-PC002
- [ ] Listan visar `redemptionCount` och `maxRedemptions` korrekt.

### UC-PC003
- [ ] Inlösning av fri biljett sätter `Ticket.Status = Paid` utan betalning.
- [ ] Inlösning av rabatt uppdaterar `finalPrice` korrekt.
- [ ] Inaktiv eller utgången kod ger 422 med korrekt felkod.
- [ ] Inlösning ökar `redemptionCount` med 1.

### UC-PC004
- [ ] Deaktivering gör att koden inte kan lösas in.
- [ ] Befintliga inlösningar påverkas inte av deaktivering.

### UC-PC005
- [ ] Historiklistan visar korrekt `discountApplied` per inlösning.

---

## Tester

### Domäntester (`ConventionSystem.Domain.Tests`)
- `PromotionCode_Redeem_IncreasesRedemptionCount`
- `PromotionCode_Redeem_WhenInactive_ThrowsDomainRuleViolation`
- `PromotionCode_Redeem_WhenMaxRedemptionsReached_ThrowsDomainRuleViolation`
- `PromotionCode_Redeem_WhenOutsideValidityPeriod_ThrowsDomainRuleViolation`
- `PromotionCode_Redeem_WhenTicketTypeNotAllowed_ThrowsDomainRuleViolation`
- `PromotionCode_Deactivate_SetsIsActiveToFalse`
- `RegistrationRuleService_ValidatePromotionCode_AllRules`

### Applikationstester (`ConventionSystem.Application.Tests`)
- `CreatePromotionCodeHandler_WithDuplicateCode_ThrowsDomainRuleViolation`
- `RedeemPromotionCodeHandler_FreeTicket_SetsTicketStatusToPaid`
- `RedeemPromotionCodeHandler_Discount_UpdatesFinalPrice`

---

## Commit-förslag

```
feat(registration): implement UC-PC001 create promotion code
feat(registration): implement UC-PC002 list promotion codes
feat(registration): implement UC-PC003 redeem promotion code
feat(registration): implement UC-PC004 deactivate promotion code
feat(registration): implement UC-PC005 promotion code redemption history
feat(admin): add promotion codes management page
feat(public): add promotion code input to ticket checkout flow
```
