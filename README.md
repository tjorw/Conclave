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

### Krav

| Verktyg | Version | Används till |
|---------|---------|--------------|
| .NET SDK | 9.0 | Backend API |
| Node.js | 22+ | Angular frontend |
| Angular CLI | 21+ | Bygga och köra Angular-apparna |
| Docker Desktop | senaste | Integrationstester (SQL Server-container) |

Docker Desktop krävs bara för integrationstesterna. Testcontainers startar en SQL Server-container automatiskt; ingen lokal SQL Server-installation behövs. SQL Server-imagen (`mcr.microsoft.com/mssql/server`, ~600 MB) hämtas första gången.

---

### Backend-API

```bash
# Bygg hela lösningen
dotnet build

# Kör API:t (lyssnar på http://localhost:5000 och https://localhost:5001)
dotnet run --project src/ConventionSystem.Api
```

**Notering:** `appsettings.Development.json` (ej incheckad) behöver innehålla connection strings och JWT-konfiguration. Skapa den lokalt med:

```json
{
  "ConnectionStrings": {
    "SystemDb": "Server=...;Database=ConventionSystemRegistry;...",
    "IdentityDb": "Server=...;Database=ConventionSystemIdentity;..."
  },
  "Jwt": {
    "Key": "minst-32-tecken-lång-hemlig-nyckel",
    "Issuer": "ConventionSystem",
    "Audience": "ConventionSystem"
  }
}
```

---

### Frontend

Frontend-projekten bor under `frontend/` och är ett Angular-workspace med två appar och ett delat bibliotek.

```
frontend/
├── projects/
│   ├── admin/    # Admin-app (port 4200)
│   ├── public/   # Publik vy (port 4201)
│   └── shared/   # Delat bibliotek: API-typer, auth, interceptors, guards
└── angular.json
```

**Förberedelse (en gång):**

```bash
cd frontend
npm install
```

**Konfigurera konventions-ID:**

Redigera `projects/admin/src/environments/environment.ts` respektive `projects/public/src/environments/environment.ts` och sätt rätt `conventionId` (det Guid som skapades vid provisioning).

**Kör apparna:**

```bash
# Admin-app på http://localhost:4200
ng serve admin

# Publik vy på http://localhost:4201
ng serve public --port 4201

# Bygg för produktion
ng build admin --configuration production
ng build public --configuration production
```

> Starta API:t först – Angular-apparna gör API-anrop mot `http://localhost:5000`.

---

### Tester

```bash
# Enhetstester och applikationstester (kräver inte Docker)
dotnet test --filter "FullyQualifiedName!~Integration"

# Alla tester inklusive integrationstester (kräver Docker Desktop)
dotnet test
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

Kärn-BC. Ansvarar för konventionens identitet, organisationsstruktur och livscykel. Hanterar upplagan (`Edition`) som är den faktiska genomföringsinstansen, samt allt som definierar dess fysiska och organisatoriska form: lokaler, funktionsområden, stationer, kategorier och personer. Alla andra BC:n refererar till id:n härifrån.

| Typ | Namn |
|---|---|
| Aggregate roots | `Convention`, `Edition` |
| Entiteter | `Person`, `ConventionAdministrator`, `Venue`, `StaffArea`, `Station`, `Category` |
| Value objects | `DatePeriod` |

**Viktiga regler:**
- `Edition` måste vara `Published` innan registrering eller evenemang kan skapas
- En `Edition` har en bemanningskoordinator och en evenemangskoordinator

### Event

Hanterar livscykeln för ett evenemang (rollspel, brädspel, föreläsning etc.) från inlämning till publicering och schemaläggning. En arrangör skapar ett utkast, fyller i sessionönskemål och skickar in för granskning. Evenemangskoordinatorn godkänner eller avvisar. Kategoriansvarig schemalägger sessioner oberoende av arrangörens önskemål.

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

Hanterar de tre registreringstyperna: besöksregistrering (vill gå på konventionen), staffansökan (vill arbeta som funktionär) och sessionsregistrering (vill delta i ett specifikt evenemang). Varje typ är ett eget aggregat med sin regeluppsättning. `RegistrationRuleService` samordnar plats- och biljettvalidering tvärs aggregat.

| Typ | Namn |
|---|---|
| Aggregate roots | `VisitorRegistration`, `SessionRegistration`, `StaffApplication`, `Ticket` |
| Entiteter | `Availability`, `StationPreference`, `TicketType`, `TicketPerk` |
| Domain service | `RegistrationRuleService` (validerar platser och biljetter) |

### Staff

Hanterar bemanningen av konventionen. Bemanningskoordinatorn skapar pass (`Shift`) på stationer, och funktionärer tilldelas via `StaffAssignment`. `AssignmentService` varnar vid överlappande pass men blockerar inte – koordinatorn har sista ordet. Staffansökan (i Registration-BC:n) är förutsättningen för att en person ska kunna tilldelas ett pass.

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
