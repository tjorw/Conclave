# Implementationsplan: Biljettrevision (R15–R18)

Planen täcker fyra arbetspaket som implementerar den reviderade biljettmodellen definierad i `uc-tickets.md` (UC-TK001–TK009). Arbetspaketens status spåras i `Roadmap.md`.

**Beroendeordning:** R15 → R16 → R17 → R18  
**Inom varje paket:** Domain → EF/Migration → Application → API → Tester

---

## Befintlig kod som påverkas

| Fil | Nuläge | Förändras i |
|-----|--------|-------------|
| `Domain/Registration/Entities/TicketType.cs` | Har `IsSellable`, `IsPubliclyVisible`; saknar `ValidDays`, `AllowedCategories` | R15 |
| `Domain/Registration/Aggregates/Ticket.cs` | `ConfirmPayment()` raiser inget event | R15 |
| `Domain/Registration/Services/IRegistrationRuleService.cs` | Synkron signatur, stub-implementation | R18 |
| `Infrastructure/Registration/StubRegistrationRuleService.cs` | Returnerar alltid `true` | R18 |
| `Application/Registration/Commands/IssueTicket/` | Admin-only, `InvalidOperationException`-typer | R15/R16 |
| `Application/Registration/Commands/RevokeTicket/` | Ingen kaskad, ingen behörighetskontroll | R16 |
| `Application/Registration/Commands/CollectTicket/` | Returnerar void, inga perks | R16 |
| `Application/Registration/Commands/ConfirmVisitorRegistrationPayment/` | Direktkommando, inte event-driven | R17 |

---

## R15 – TicketType-domänrevision

### Domain

**`TicketType.cs`** – ÄNDRAS
- Ta bort `IsSellable`, `IsPubliclyVisible`
- Lägg till `ValidDays: IReadOnlyList<DateOnly>?` (backing field `_validDays`)
- Lägg till `AllowedCategories: Guid[]?` (CategoryId är cross-context, lagras som `Guid`)
- Uppdatera konstruktor och `Update()`-signatur
- Validering av `ValidDays` mot `Edition.Period` sker i **Application-lagret** (handlern), inte i domänklassen – domänklassen saknar tillgång till `Edition`-aggregatet

**`Ticket.cs`** – ÄNDRAS
- `ConfirmPayment()`: lägg till `RaiseDomainEvent(new TicketPaid(Id, PersonId, EditionId, DateTimeOffset.UtcNow))`
- Ny metod `CancelOwn()`: kastar `TicketNotReservedForCancellationException` om `Status != Reserved`; sätter `Status = Revoked`; raiser `TicketRevoked` (med `performedById = PersonId`)

**`RegistrationEvents.cs`** – ÄNDRAS
```csharp
public record TicketPaid(
    TicketId TicketId,
    PersonId PersonId,
    EditionId EditionId,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

**`RegistrationRuleExceptions.cs`** – ÄNDRAS
- Lägg till `TicketValidDaysOutsideEditionPeriodException : DomainRuleViolationException`
- Lägg till `TicketNotReservedForCancellationException : DomainRuleViolationException`
- Lägg till `TicketAlreadyPaidException : DomainRuleViolationException`

### EF Core + Migration

**Migration `ReviseTicketTypeValidDays`:**
- `ticket_types`: DROP `is_sellable`, DROP `is_publicly_visible`
- `ticket_types`: ADD `valid_days nvarchar(max) NULL`
- `ticket_types`: ADD `allowed_categories nvarchar(max) NULL`

**`TicketTypeConfiguration.cs`** – ÄNDRAS
- Ta bort konfiguration för `is_sellable`, `is_publicly_visible`
- Lägg till JSON-serialisering via `HasConversion<string>` + `JsonSerializer` för `valid_days` och `allowed_categories`

### Application

**`CreateTicketTypeCommand/Handler`** – ÄNDRAS
- Ny signatur: ta bort `IsSellable`/`IsPubliclyVisible`, lägg till `DateOnly[]? ValidDays`, `Guid[]? AllowedCategories`
- Handlern laddar `Edition` och validerar att alla datum i `ValidDays` faller inom `edition.Period`; kastar `TicketValidDaysOutsideEditionPeriodException` vid fel

**`UpdateTicketTypeCommand/Handler`** – ÄNDRAS
- Samma signaturändring som Create

**Alla befintliga handlers i Registration** – ÄNDRAS
- Byt `InvalidOperationException` → `ResourceNotFoundException` (ej funnen), `ForbiddenException` (ej behörig), `DomainRuleViolationException` (domänregel)

### Tester

**Ny `Domain.Tests/Registration/TicketTypeTests.cs`**
- Konstruktor sätter `ValidDays`
- `null` är tillåtet för `ValidDays` och `AllowedCategories`
- `Update()` uppdaterar `ValidDays`

**`Domain.Tests/Registration/TicketTests.cs`** – ÄNDRAS
- Lägg till: `ConfirmPayment_RaisesTicketPaidEvent`

**`Application.Tests/.../CreateTicketTypeHandlerTests.cs`** – ÄNDRAS
- Uppdatera `Setup()` (ny konstruktorsignatur)
- Lägg till: `Handle_ValidDayOutsideEditionPeriod_Throws`

**`Application.Tests/.../UpdateTicketTypeHandlerTests.cs`** – ÄNDRAS
- Uppdatera command-anrop

**`Application.Tests/.../IssueTicketHandlerTests.cs`** – ÄNDRAS
- Byt `InvalidOperationException` mot `ForbiddenException`/`ResourceNotFoundException`

---

## R16 – Nya commands + kaskad + CollectTicket-svar

### Nya filer

**`Commands/AddTicketPerk/AddTicketPerkCommand.cs` + Handler** – SKAPAS (UC-TK002)
- `record AddTicketPerkCommand(Guid TicketTypeId, string Description) : IRequest<Guid>`
- Handler: hämtar `TicketType`, kontrollerar admin-behörighet, anropar `ticketType.AddPerk()`, returnerar perk-id
- `ITicketTypeRepository` utökas med `void MarkAsAdded<T>(T entity) where T : class`

**`Commands/AssignTicket/AssignTicketCommand.cs` + Handler** – SKAPAS (UC-TK003, ersätter IssueTicket)
- `record AssignTicketCommand(Guid PersonId, Guid EditionId, Guid TicketTypeId) : IRequest<Guid>`
- Handler: accepterar admin, `edition.EventCoordinatorId == currentUser.PersonId`, eller `edition.StaffCoordinatorId == currentUser.PersonId`; kastar `ForbiddenException` annars

**`Commands/CancelOwnTicket/CancelOwnTicketCommand.cs` + Handler** – SKAPAS (UC-TK006)
- `record CancelOwnTicketCommand(Guid TicketId) : IRequest`
- Handler: kontrollerar `ticket.PersonId == currentUser.PersonId` → `ForbiddenException`; anropar `ticket.CancelOwn()`

**`Commands/CollectTicket/CollectTicketResult.cs`** – SKAPAS (UC-TK008)
```csharp
public sealed record CollectTicketResult(IReadOnlyList<TicketPerkDto> Perks);
```

**`DomainEventHandlers/TicketRevokedHandler.cs`** – SKAPAS (UC-TK007 kaskad)
- Lyssnar på `TicketRevoked`
- Hämtar alla bekräftade `SessionRegistrations` via `GetAllConfirmedByTicketIdAsync(notification.TicketId)`
- Anropar `Cancel()` på var och en; sparar
- `ISessionRegistrationRepository` utökas med `Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedByTicketIdAsync(TicketId, CancellationToken)`

### Ändringar

**`CollectTicketCommand.cs`** – `IRequest` → `IRequest<CollectTicketResult>`

**`CollectTicketHandler.cs`** – ÄNDRAS
- Injicera `ITicketTypeRepository`
- Efter `ticket.Collect()`: hämta `ticketType`, returnera `CollectTicketResult` med dess Perks

**`RevokeTicketHandler.cs`** – ÄNDRAS
- Lägg till admin-behörighetskontroll (hämta convention, kontrollera `IsAdministrator`)

**`RegistrationEndpoints.cs`** – ÄNDRAS
- `POST /ticket-types/{id}/perks` – AddTicketPerk
- `POST /editions/{id}/tickets` skickar nu `AssignTicketCommand` (byt ut IssueTicket)
- `DELETE /tickets/{id}/cancel-own` – CancelOwnTicket
- Collect-endpoint: `Results.Ok(result)` istället för `Results.NoContent()`

### Tester (nya)
- `AddTicketPerkHandlerTests.cs`: lyckligt flöde, ej funnen, ej behörig
- `AssignTicketHandlerTests.cs`: admin, EventCoordinator, StaffCoordinator, obehörig
- `CancelOwnTicketHandlerTests.cs`: Reserved-biljett, Paid-biljett kastar, annans biljett kastar
- `TicketRevokedHandlerTests.cs`: avbokar alla kopplade registreringar, inga registreringar → sparar ej

### Tester (uppdateras)
- `CollectTicketHandlerTests.cs`: ny returtyp, assertions för Perks
- `RevokeTicketHandlerTests.cs`: lägg till `NonAdmin_Throws_Forbidden`

---

## R17 – Betalningsflöde + UC-VR002-revision

### Nya filer

**`Commands/RegisterManualPayment/RegisterManualPaymentCommand.cs` + Handler** – SKAPAS (UC-TK004)
- `record RegisterManualPaymentCommand(Guid TicketId) : IRequest`
- Handler: admin-only; kastar `TicketAlreadyPaidException` om status är Paid eller Collected; anropar `ticket.ConfirmPayment()` (raiser `TicketPaid`)

**`Commands/ConfirmWebhookPayment/ConfirmWebhookPaymentCommand.cs` + Handler** – SKAPAS (UC-TK005)
- `record ConfirmWebhookPaymentCommand(string ExternalReference) : IRequest`
- Handler: ej autentiserat; slår upp `VisitorRegistration` via `GetByExternalReferenceAsync`; idempotent om ej funnen eller redan bekräftad; anropar `registration.ConfirmPayment()` + `ticket.ConfirmPayment()`
- `IVisitorRegistrationRepository` utökas med `GetByExternalReferenceAsync(string, CancellationToken)`

**`DomainEventHandlers/TicketPaidHandler.cs`** – SKAPAS (UC-VR002 reaktivt flöde)
- Lyssnar på `TicketPaid`
- Hämtar kopplad `VisitorRegistration` via `GetByTicketIdAsync`; returnerar om ingen finns (admin-assigned biljett)
- Anropar `registration.ConfirmPayment()` om status är `PendingPayment`
- `IVisitorRegistrationRepository` utökas med `GetByTicketIdAsync(TicketId, CancellationToken)`

### Ändringar

**`RegistrationEndpoints.cs`** – ÄNDRAS
- `POST /tickets/{id}/register-payment` – RequireAuthorization("IsAdmin")
- `POST /webhooks/payment-confirmed` – ingen auth, webhook-signaturvalidering

Det gamla `ConfirmVisitorRegistrationPaymentCommand` bevaras men är inte längre primärt flöde.

### Tester (nya)
- `RegisterManualPaymentHandlerTests.cs`: lyckligt flöde, redan betald kastar, obehörig kastar, ej funnen kastar
- `ConfirmWebhookPaymentHandlerTests.cs`: lyckligt flöde, idempotens (okänd referens), idempotens (redan bekräftad)
- `TicketPaidHandlerTests.cs`: bekräftar VisitorRegistration, inget händer om ingen registrering, idempotens

---

## R18 – Real RegistrationRuleService

### Breaking change: asynkron IRegistrationRuleService

**`IRegistrationRuleService.cs`** – ÄNDRAS
```csharp
public interface IRegistrationRuleService
{
    Task<bool> ValidateSeatAvailability(SessionId sessionId, CancellationToken ct = default);
    Task<bool> ValidateTicket(PersonId personId, TicketId ticketId, SessionId sessionId, CancellationToken ct = default);
}
```

Påverkar: `StubRegistrationRuleService`, `RegisterForSessionHandler`, alla tester som mockar tjänsten.

### Kors-kontextkommunikation

**`Application/Registration/Abstractions/ISessionInfoService.cs`** – SKAPAS
```csharp
public interface ISessionInfoService
{
    Task<SessionInfo?> GetSessionInfoAsync(SessionId sessionId, CancellationToken ct = default);
}

public sealed record SessionInfo(
    SessionId SessionId,
    DateOnly SessionDate,
    Guid CategoryId,
    EditionId EditionId);
```

Lokalt read-model i Registration-kontexten – inget beroende mot Event-domänen.

**`Infrastructure/Registration/EventContextSessionInfoService.cs`** – SKAPAS
- Implementerar `ISessionInfoService`
- Projicerar direkt via LINQ mot `ConventionDbContext` (ingen Event-aggregat-inläsning)
- Returnerar `SessionInfo` med datum (`DateOnly.FromDateTime(session.TimeSlot.Start)`) och `CategoryId`

### ValidateTicket-logik

**`Infrastructure/Registration/RegistrationRuleService.cs`** – SKAPAS (ersätter stub)

Steg i ordning:
1. Hämta `sessionInfo` via `ISessionInfoService` → om null: `false`
2. Person har ≥1 biljett med status `Paid`/`Collected` för `sessionInfo.EditionId` → om inte: `false`
3. Hämta biljetttypens `ValidDays` och `AllowedCategories` via biljettens `TicketTypeId`
4. `ValidDays != null` → sessionens datum måste finnas i arrayen → om inte: `false`
5. `AllowedCategories != null` → `sessionInfo.CategoryId` måste finnas i arrayen → om inte: `false`
6. Returnera `true`

### ValidateSeatAvailability-logik

```csharp
var maxSeats = // projektion: session.MaxSeats via ConventionDbContext
var confirmedCount = await db.SessionRegistrations
    .CountAsync(r => r.SessionId == sessionId &&
                     r.Status == SessionRegistrationStatus.Confirmed, ct);
return maxSeats == 0 || confirmedCount < maxSeats;
```

### Ändringar

**`StubRegistrationRuleService.cs`** – ÄNDRAS: asynkrona signaturer (`Task.FromResult(true)`)

**`RegisterForSessionHandler.cs`** – ÄNDRAS
- `await` för bägge regelanrop
- Läs `personId` från `ticket.PersonId` (inte från command) – fix av tech-debt `PersonId från klient`

**`InfrastructureServiceExtensions.cs`** – ÄNDRAS
- Registrera `services.AddScoped<ISessionInfoService, EventContextSessionInfoService>()`
- Byt stub mot `services.AddScoped<IRegistrationRuleService, RegistrationRuleService>()`

### Tester (nya)
- `RegistrationRuleServiceTests.cs`: giltig biljett, fel dag, fel kategori, ingen biljett, full session, ledig session

### Tester (uppdateras)
- `RegisterForSessionHandlerTests.cs`: asynkrona mock-setup för `IRegistrationRuleService`

---

## Sammanfattning

| Paket | Nya filer | Ändrade filer | Migration |
|-------|-----------|---------------|-----------|
| R15 | 1 (TicketTypeTests) | ~10 | Ja |
| R16 | ~8 | ~6 | Nej |
| R17 | ~6 | ~4 | Nej |
| R18 | ~3 | ~5 | Nej |

### Breaking changes

| Förändring | Åtgärd |
|------------|--------|
| `TicketType`-konstruktor/-`Update` ny signatur | Uppdatera alla handlertester som skapar `TicketType` direkt i `Setup()` |
| `Ticket.ConfirmPayment()` raiser `TicketPaid` | Lägg till event-assertion i befintliga tester |
| `CollectTicketCommand` ändrar returtyp | Uppdatera endpoint och handlertester |
| `IRegistrationRuleService` asynkron | Uppdatera stub, handler och mock-setup i tester |
