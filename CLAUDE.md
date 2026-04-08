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
│   │   └── Volunteer/
│   ├── ConventionSystem.Application/     # Use cases, commands, queries (CQRS)
│   │   ├── Convention/
│   │   ├── Event/
│   │   ├── Registration/
│   │   └── Volunteer/
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
③ Domain        – Convention | Event | Registration | Volunteer
④ Infrastructure – EF Core, repositories, identity, extern auth, e-post
```

Beroendet pekar alltid inåt. Infrastructure beror på Domain, aldrig tvärtom.

### Bounded Contexts och kommunikation

De fyra contexts kommunicerar via domain events och id-referenser – ingen direkt koppling mellan aggregat:

- **Event** läser: `ConventionId`, `EditionId`, `CategoryId`, `VenueId` från Convention
- **Registration** läser: `ConventionId`, `EditionId`, `PersonId` från Convention
- **Volunteer** läser: `StationId` från Convention; `PersonId` från Registration

**Viktiga domain event-flöden:**
- `EditionPublished` – startsignal för Event och Registration
- `SessionDeactivated` / `EventCancelled` → avbokning av sessionsregistreringar
- `VolunteerApplicationReceived` → notifiering till volontärkoordinator
- `VolunteerShiftCancelled` → automatisk avbokning av tilldelningar

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
Aggregate roots: `VisitorRegistration`, `SessionRegistration`, `VolunteerApplication`, `Ticket`  
Entiteter: `Availability`, `StationPreference`, `TicketType`, `TicketPerk`  
Domain service: `RegistrationRuleService` (validerar platser och biljetter)

### Volunteer
Aggregate root: `VolunteerShift`  
Entiteter: `VolunteerAssignment`  
Value objects: `StaffingRequirement`, `TimeSlot`  
Domain service: `AssignmentService` (kontrollerar överlapp – varning, blockerar inte)
