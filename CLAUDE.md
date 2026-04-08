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
- `DomainEventLog` – lyssnar på alla domain events och persisterar dem med `conventionId`, `eventType`, `payload`, `performedById`, `occurredAt`

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
