# Conclave

System för att administrera, annonsera, registrera och driva hobbymässor (tabletop gaming) i Sverige.

## Teknikstack

- **Backend:** .NET 9, C#
- **Arkitektur:** Clean Architecture med DDD (Domain-Driven Design)
- **ORM:** Entity Framework Core
- **Databas:** SQL Server (deploy-per-konvention – en databas per instans, `dbo`-schema för domändata, `identity`-schema för ASP.NET Identity)
- **Frontend:** Angular (admin-app + publik vy)
- **Auth:** ASP.NET Identity med JWT
- **API:** REST, minimal API

## Kom igång

### Krav

| Verktyg | Version | Används till |
|---------|---------|--------------|
| .NET SDK | 9.0 | Backend API |
| SQL Server | valfri lokal instans | Konventionsdatabas (dbo + identity-schema) |
| Node.js | 22+ | Angular frontend |
| Angular CLI | 21+ | Bygga och köra Angular-apparna |
| Docker Desktop | senaste | Integrationstester (SQL Server-container) |

Docker Desktop krävs bara för integrationstesterna. Testcontainers startar en SQL Server-container automatiskt; ingen lokal SQL Server-installation behövs för CI. SQL Server-imagen (`mcr.microsoft.com/mssql/server`, ~600 MB) hämtas första gången.

---

### Första gången – från kod till inloggning

#### Steg 1 – Skapa `appsettings.Development.json`

Filen är gitignorerad och måste skapas lokalt. Lägg den i `backend/src/ConventionSystem.Api/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ConventionSystem;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "minst-32-tecken-lång-hemlig-nyckel-byt-ut-mig",
    "Issuer": "ConventionSystem",
    "Audience": "ConventionSystem"
  }
}
```

#### Steg 2 – Starta API:t

```bash
dotnet run --project backend/src/ConventionSystem.Api
```

API:t lyssnar på `http://localhost:5127`. Databasen `ConventionSystem` skapas och migreras automatiskt vid uppstart (`dbo`-schema via ConventionDbContext, `identity`-schema via ApplicationIdentityDbContext).

I `Development`-miljön körs seedern automatiskt och skapar en komplett demo-konvention om den inte redan finns. I konsolen loggas konventions-ID:t:

```
Seeder: demo-konvention skapad (id=<GUID>). Logga in med admin@demo.se / Admin123!
```

Demo-konventionen innehåller en publicerad upplaga med lokaler, funktionsområden, stationer och kategorier ifyllda.

#### Steg 3 – Konfigurera frontend-miljön

`environment.ts` är gitignorerad (innehåller lokalt `conventionId`). Kopiera exempelfilerna och fyll i GUID:t från konsolen:

```bash
# Admin-app
cp frontend/projects/admin/src/environments/environment.ts.example \
   frontend/projects/admin/src/environments/environment.ts

# Publik app
cp frontend/projects/public/src/environments/environment.ts.example \
   frontend/projects/public/src/environments/environment.ts
```

Öppna båda filerna och ersätt placeholder-värdet med konventions-ID:t från konsolen:

```typescript
conventionId: '<GUID från konsolen>',
```

#### Steg 4 – Installera npm-paket (en gång)

```bash
cd frontend
npm install
```

#### Steg 5 – Starta apparna

```bash
# Admin-app (port 4200)
ng serve admin

# Publik app (port 4201, i separat terminal)
ng serve public
```

Öppna `http://localhost:4200` för admin-appen – logga in med `admin@demo.se / Admin123!`

Öppna `http://localhost:4201` för den publika appen.

---

### Daglig utveckling

```bash
# Terminal 1 – API
dotnet run --project backend/src/ConventionSystem.Api

# Terminal 2 – Admin-app
cd frontend && ng serve admin

# Terminal 3 – Publik app (vid behov)
cd frontend && ng serve public
```

---

### Tester

```bash
# Enhetstester och applikationstester (kräver inte Docker)
dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName!~Integration"

# Alla tester inklusive integrationstester (kräver Docker Desktop)
dotnet test backend/ConventionSystem.sln
```

## Lösningsstruktur

```
/
├── backend/
│   ├── ConventionSystem.sln
│   ├── src/
│   │   ├── ConventionSystem.Domain/          # Domänlager – inga beroenden utåt
│   │   │   ├── Convention/
│   │   │   ├── Event/
│   │   │   ├── Registration/
│   │   │   └── Staff/
│   │   ├── ConventionSystem.Application/     # Use cases, commands, queries (CQRS)
│   │   │   ├── Convention/
│   │   │   ├── Event/
│   │   │   ├── Registration/
│   │   │   └── Staff/
│   │   ├── ConventionSystem.Infrastructure/  # EF Core, repositories, identity, extern auth, e-post
│   │   └── ConventionSystem.Api/             # Minimal API-endpoints
│   └── tests/
│       ├── ConventionSystem.Domain.Tests/
│       ├── ConventionSystem.Application.Tests/
│       └── ConventionSystem.Integration.Tests/
├── frontend/
│   └── projects/
│       ├── admin/     # Admin-app – rollbaserad, port 4200 (Angular Material)
│       ├── public/    # Publik vy – konventionsbrandad, port 4201 (Angular Material)
│       └── shared/    # Delat bibliotek: API-typer, tjänster, auth, interceptors
└── docs/
```

Beroendet pekar alltid inåt: Infrastructure → Application → Domain.

## Arkitektur

### Systemnivå

Tre infrastrukturskikt:

- **Klienter:** Admin-app (Angular, rollbaserad), publik vy (Angular, konventionsstyld), externt CMS (REST-feed, läsbart)
- **API-lager (.NET):** Auth (JWT + OAuth), publik REST (feed + webhooks)
- **Datanivå:** En databas per deploy – domändata i `dbo`-schema, ASP.NET Identity i `identity`-schema

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
| Entiteter | `Session`, `SessionRequest`, `CoOrganiser`, `EventComment` |
| Value objects | `TimeSlot` |

**Viktiga regler:**
- Innehållsfälten (titel, beskrivning, registreringstyp) och sessionönskemål lagras direkt på `Event` och är redigerbara i `Draft`-status
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
