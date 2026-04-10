# Conclave

System för att administrera, annonsera, registrera och driva hobbymässor (tabletop gaming) i Sverige.

## Teknikstack

- **Backend:** .NET 9, C#
- **Arkitektur:** Clean Architecture med DDD (Domain-Driven Design)
- **ORM:** Entity Framework Core
- **Databas:** SQL Server (multi-tenant – en databas per konvention + systemdatabas + identitetsdatabas)
- **Frontend:** Angular *(ej påbörjat)*
- **Auth:** ASP.NET Identity med OAuth *(ej påbörjat)*
- **API:** REST, minimal API

## Kom igång

```bash
dotnet build
dotnet test
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
│   └── ConventionSystem.Api/             # Minimal API-endpoints
└── tests/
    ├── ConventionSystem.Domain.Tests/
    └── ConventionSystem.Application.Tests/
```

Beroendet pekar alltid inåt: Infrastructure → Application → Domain.

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

## Domänmodell

Systemet är indelat i fyra bounded contexts som kommunicerar via domain events och id-referenser – ingen direkt koppling mellan aggregat.

### Convention

| Typ | Namn |
|---|---|
| Aggregate roots | `Convention`, `Edition` |
| Entiteter | `Person`, `ConventionAdministrator`, `Venue`, `StaffArea`, `Station`, `Category` |
| Value objects | `DatePeriod` |

**Viktiga regler:**
- `Edition` måste vara `Published` innan registrering eller evenemang kan skapas
- En `Edition` har en bemanningskoordinator och en evenemangskoordinator

### Event

| Typ | Namn |
|---|---|
| Aggregate root | `Event` |
| Entiteter | `EventVersion`, `Session`, `SessionRequest`, `CoOrganiser`, `EventComment` |
| Value objects | `TimeSlot` |

**Viktiga regler:**
- Ett evenemang har alltid ett `DraftVersionId` (utkast) och eventuellt ett `PublishedVersionId` (publicerad version) – cirkulär FK-referens med `NoAction` på delete
- `SessionRequest` har ingen koppling till `Session` – kategoriansvarig äger schemat och behöver inte följa requests

**Livscykel:** `Draft` → `UnderReview` → `Published` (eller `Cancelled`)

### Registration

| Typ | Namn |
|---|---|
| Aggregate roots | `VisitorRegistration`, `SessionRegistration`, `StaffApplication`, `Ticket` |
| Entiteter | `Availability`, `StationPreference`, `TicketType`, `TicketPerk` |
| Domain service | `RegistrationRuleService` (validerar platser och biljetter) |

### Staff

| Typ | Namn |
|---|---|
| Aggregate root | `Shift` |
| Entiteter | `StaffAssignment` |
| Value objects | `StaffingRequirement`, `TimeSlot` |
| Domain service | `AssignmentService` (kontrollerar överlapp – varning, blockerar inte) |

## Bounded context-kommunikation

Contexts läser id-referenser från varandra men anropar aldrig varandras aggregat direkt:

- **Event** läser: `ConventionId`, `EditionId`, `CategoryId`, `VenueId` från Convention
- **Registration** läser: `ConventionId`, `EditionId`, `PersonId` från Convention
- **Staff** läser: `StationId` från Convention; `PersonId` från Registration

**Viktiga domain event-flöden:**

| Event | Utlöser |
|---|---|
| `EditionPublished` | Startsignal för Event och Registration |
| `SessionDeactivated` / `EventCancelled` | Avbokning av sessionsregistreringar |
| `StaffApplicationReceived` | Notifiering till bemanningskoordinator |
| `ShiftCancelled` | Automatisk avbokning av tilldelningar |

## Domain events

Domain events dispatchar via MediatR efter lyckad `SaveChanges` och loggas alltid till `domain_event_log`-tabellen i samma transaktion. Skapa en handler genom att implementera `IDomainEventHandler<T>`:

```csharp
public class EditionPublishedHandler : IDomainEventHandler<EditionPublished>
{
    public async Task Handle(EditionPublished notification, CancellationToken ct)
    {
        // ...
    }
}
```

Handlers i `ConventionSystem.Application` registreras automatiskt.
