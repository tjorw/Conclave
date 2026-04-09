# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Språkkonvention

- **Kod och modellering:** Engelska – klassnamn, metoder, properties, variabler, namnrymder, databaskolumner
- **Dokumentation och resonemang:** Svenska – kommentarer, README, commit-meddelanden, svar i konversationen

## Teknikstack

- **Backend:** .NET 9, C#
- **Arkitektur:** Clean Architecture med DDD (Domain-Driven Design)
- **ORM:** Entity Framework Core
- **Databas:** SQL Server (multi-tenant – en databas per konvention + systemdatabas + identitetsdatabas)
- **Frontend:** Angular (separata appar för admin och publik vy)
- **Auth:** ASP.NET Identity med stöd för social inloggning (OAuth)
- **API:** REST, minimal API-endpoints

## Byggkommandon

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~Convention"   # kör tester för ett specifikt bounded context
dotnet run --project src/ConventionSystem.Api
```

## Lösningsstruktur

```
ConventionSystem.sln
├── src/
│   ├── ConventionSystem.Domain/          # Domänlager – inga beroenden utåt
│   │   ├── Convention/
│   │   ├── Event/
│   │   ├── Registration/
│   │   └── Staff/
│   ├── ConventionSystem.Application/     # Use cases, commands, queries (CQRS)
│   │   ├── Convention/
│   │   ├── Event/
│   │   ├── Registration/
│   │   └── Staff/
│   ├── ConventionSystem.Infrastructure/  # EF Core, repositories, identity, extern auth, e-post
│   └── ConventionSystem.Api/             # Controllers, minimal API-endpoints, feed-endpoints
└── tests/
    ├── ConventionSystem.Domain.Tests/
    └── ConventionSystem.Application.Tests/
```

## Arkitektur

### Systemnivå

Tre infrastrukturskikt:

- **Klienter:** Admin-app (Angular, rollbaserad), publik vy (Angular, konventionsstyld), externt CMS (REST-feed, läsbart)
- **API-lager (.NET):** Tenant-router (löser rätt databas per request via domän/header), Auth (JWT + OAuth), publik REST (feed + webhooks)
- **Datanivå:** Tenant-databaser (en per konvention), systemdatabas (tenant-register och routing), identitetsdatabas (konton och autentisering)

### Clean Architecture-lager

```
① Presentation  – Controllers, minimal API, feed-endpoints
② Application   – Use cases, commands, queries (CQRS), validering
③ Domain        – Convention | Event | Registration | Staff
④ Infrastructure – EF Core, repositories, identity, extern auth, e-post
```

Beroendet pekar alltid inåt. Infrastructure beror på Domain, aldrig tvärtom.

### Bounded Contexts och kommunikation

De fyra contexts kommunicerar via domain events och id-referenser – ingen direkt koppling mellan aggregat:

- **Event** läser: `ConventionId`, `EditionId`, `CategoryId`, `VenueId` från Convention
- **Registration** läser: `ConventionId`, `EditionId`, `PersonId` från Convention
- **Staff** läser: `StationId` från Convention; `PersonId` från Registration

**Viktiga domain event-flöden:**
- `EditionPublished` – startsignal för Event och Registration
- `SessionDeactivated` / `EventCancelled` → avbokning av sessionsregistreringar
- `StaffApplicationReceived` → notifiering till bemanningskoordinator
- `ShiftCancelled` → automatisk avbokning av tilldelningar

## Domänkonventioner

- **Private setters** på alla properties
- **Konstruktorer som enforcar invarianter**
- **Domain events** samlas i en lista på aggregate root och publiceras via infrastrukturlagret
- **Starka id-typer:** `ConventionId`, `PersonId`, `EditionId` etc. – wrappade `Guid`
- **ID-generering:** `Guid.CreateVersion7()` (.NET 9) i applikationskod innan insert. EF Core konfigureras med `HasDefaultValueSql("newsequentialid()")` som fallback på databasnivå. Generera aldrig id i databasen.
- **Monetära belopp:** `int` (ören) eller `decimal`
- `DomainEventLog` – alla domain events serialiseras till JSON och sparas i `domain_event_log`-tabellen i samma transaktion som aggregatändringen, innan MediatR-dispatch

## Domänmodell – översikt

### Convention
Aggregate roots: `Convention`, `Edition`  
Entiteter: `Person`, `ConventionAdministrator`, `Venue`, `Station`, `Category`  
Value objects: `DatePeriod`  
Invariant: Edition måste vara `Published` innan någon registrering kan öppnas.

### Event
Aggregate root: `Event`  
Entiteter: `EventVersion`, `Session`, `SessionRequest`, `CoOrganiser`, `EventComment`  
Value objects: `TimeSlot`  
OBS: `publishedVersionId` och `draftVersionId` är nullable FK:er med cirkulär referens – hantera med nullable i EF Core och korrekt migreringsordning.  
OBS: `SessionRequest` har ingen koppling till `Session` – kategoriansvarig äger schemat och behöver inte följa requests.

### Registration
Aggregate roots: `VisitorRegistration`, `SessionRegistration`, `StaffApplication`, `Ticket`  
Entiteter: `Availability`, `StationPreference`, `TicketType`, `TicketPerk`  
Domain service: `RegistrationRuleService` (validerar platser och biljetter)

### Staff
Aggregate root: `Shift`  
Entiteter: `StaffAssignment`  
Value objects: `StaffingRequirement`, `TimeSlot`  
Domain service: `AssignmentService` (kontrollerar överlapp – varning, blockerar inte)


# Kodkonventioner

## Allmänt (alla lager)

- Alla klasser är `sealed` om de inte är basklasser
- `file-scoped namespaces` (`namespace X.Y;`)
- Primära konstruktorer för dependency injection (`sealed class Foo(IBar bar)`)
- Felmeddelanden på svenska
- `DateTimeOffset.UtcNow` – aldrig `DateTime.Now`
- `async/await` med `CancellationToken ct` som sista parameter, alltid med defaultvärde `= default` i interface-signaturer

## Domänlager

### Starka id-typer
`readonly record struct` med en statisk `New()`-metod. Id genereras alltid i applikationskod – aldrig av databasen.

```csharp
public readonly record struct ConventionId(Guid Value)
{
    public static ConventionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
```

### Aggregate roots
- Ärver `AggregateRoot` (inte `Entity<TId>`)
- Privat parameterlös konstruktor för EF Core
- Alla properties har `private set`
- Publika samlingar exponeras som `IReadOnlyList<T>`, interna fält som `private readonly List<T>`
- Invarianter enforças i konstruktorn med `throw` – inget halvinitierat tillstånd
- Raisar domain events via `RaiseDomainEvent(...)`

### Entiteter
- Ärver `Entity<TId>`
- Konstruktor märkt `internal` – entiteter skapas bara inifrån sitt aggregate root
- Muteringsmetoder (`Update`, `Deactivate` etc.) är `internal` av samma skäl
- Aggregate root-metoden ansvarar för att anropa entitetens metoder och raisa rätt events

### Value objects
- Ärver `ValueObject`
- Immutabla – properties är `{ get; }` utan setter
- Invarianter enforças i konstruktorn
- Implementerar `GetEqualityComponents()` för värdebaserad likhet

### Domain events
- `record` som implementerar `IDomainEvent`
- Namngivna i dåtid (`PersonDeactivated`, `EditionPublished`)
- Innehåller alltid `OccurredAt`
- Bär starka id-typer, aldrig råa `Guid`
- Två syften: reaktiva triggers i andra bounded contexts, och observerbara bevis på domänbeslut i tester

## Applikationslager

### Commands
- `sealed record` som implementerar `IRequest<TResult>`
- Returnerar `Guid` vid skapande, inget (`IRequest`) vid mutationer
- Bär råa `Guid` – konvertering till starka id-typer sker i handler
- Namngivna efter use case, inte CRUD (`CreatePerson`, `DeactivatePerson`)

### Handlers
- `sealed class` med primär konstruktor
- Ansvar i ordning: ladda aggregat → validera affärsregler → anropa domänmetod → spara
- Kastar `InvalidOperationException` med svenska felmeddelanden vid regelbrott

### Repository-interfaces
- Definieras i applikationslagret under `{BoundedContext}/Abstractions/`
- En interface per aggregate root
- Metodsignaturer inkluderar alltid `CancellationToken ct = default`
- Arbetar med starka id-typer, inte råa `Guid`

### Domain event handlers
- Implementerar `IDomainEventHandler<TEvent>` (alias för `INotificationHandler<TEvent>`)
- Placeras i applikationslagret under relevant bounded context

## Infrastrukturlager

### EF Core-konfigurationer
- En klass per aggregate/entitet, implementerar `IEntityTypeConfiguration<T>`
- Placeras under `Persistence/Configurations/{BoundedContext}/`
- Tabeller och kolumner namnges med `snake_case`
- Alla starka id-typer konverteras explicit med `HasConversion`
- Shadow properties används för FK:er som inte exponeras på domänklassen

```csharp
builder.ToTable("persons");
builder.Property(p => p.Id)
    .HasConversion(id => id.Value, value => new PersonId(value))
    .HasDefaultValueSql("newsequentialid()");
```

### Repositories
- `sealed class` med primär konstruktor som tar `ConventionDbContext`
- Anropar `SaveChangesAsync` i slutet av varje skrivmetod – inget unit-of-work exponeras utåt
- `EventDispatchInterceptor` sköter event-dispatch automatiskt vid `SaveChanges`

## API-lager

### Endpoints
- Statiska klasser med en `Map*Endpoints`-metod som tar och returnerar `IEndpointRouteBuilder`
- Registreras i `Program.cs` som `app.Map*Endpoints()`
- Request-typer definieras som `record` i samma fil
- URL-hierarki: skapande under förälder (`POST /conventions/{id}/persons`), mutationer på känd resurs (`PUT /persons/{id}`)
- Returkoder: `201 Created` med location-header vid skapande, `204 No Content` vid mutation

## Tester

### Domäntester
- Testar aggregate root-beteende direkt utan mockar
- Testar att rätt events raisas, att invarianter kastas, att state förändras korrekt
- `ClearDomainEvents()` anropas efter setup-steg för att isolera events från det specifika anropet under test

```csharp
convention.ClearDomainEvents();
convention.DeactivatePerson(person);
var evt = convention.DomainEvents.OfType<PersonDeactivated>().Single();
```

### Applikationstester
- Handlers testas med mockade repositories via NSubstitute
- Aggregat skapas direkt (ingen mock) – domänlogiken är riktig, bara persistensen mockas
- En testklass per handler

# Commit Strategy

## General Rules
- Never commit automatically. Always ask before committing.
- Never commit partial or broken work.
- Each commit should represent a complete, coherent unit of work.

## When to Ask About Committing
Ask the user "Ready to commit? Suggested message: [message]" when:
- A complete use case is implemented (domain, application, infrastructure, API and tests)
- A self-contained refactoring is complete
- A structural change is complete (e.g. solution setup, folder structure)

Do not ask about committing after:
- Implementing only part of a use case
- Adding a single class or file that is not yet usable
- Making a change the user has not confirmed they are happy with

## Commit Message Format
Use conventional commits:

```
<type>(<scope>): <short description in English>

[optional body in Swedish explaining why, not what]
```

**Types:**
- `feat` – new functionality
- `fix` – bug fix
- `refactor` – restructuring without behaviour change
- `test` – adding or updating tests
- `docs` – documentation only
- `chore` – tooling, dependencies, config

**Scope** maps to bounded context or layer:
- `convention`, `event`, `registration`, `staff`
- `infrastructure`, `api`, `domain`

**Examples:**
```
feat(convention): implement UC001 create convention
feat(event): implement UC-EV003 submit event for review
test(convention): add unit tests for Edition.Publish invariants
refactor(domain): extract TimeSlot value object to shared kernel
```

## What Belongs in One Commit
A use case commit should include:
- Domain changes (aggregate methods, domain events, value objects)
- Application layer (command, command handler, validator)
- Infrastructure changes (EF Core configuration, migrations if applicable)
- API endpoint
- Unit tests for domain and application layer

## What Should Never Be in One Commit
- Multiple unrelated use cases
- Commented-out code
- Failing tests
- TODO comments that refer to unimplemented required behaviour
