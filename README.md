# Conclave

System för att administrera, annonsera, registrera och driva hobbymässor (tabletop gaming) i Sverige.

## Teknikstack

| Lager | Teknologi |
|---|---|
| Backend | [.NET 9](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), C# – Clean Architecture med DDD |
| ORM | [Entity Framework Core 9](https://learn.microsoft.com/en-us/ef/core/) |
| Databas | SQL Server – en databas per deploy (`dbo` för domändata, `identity` för ASP.NET Identity) |
| Frontend | [Angular 21](https://angular.dev/) – admin-app + publik vy, standalone components, signals |
| UI-bibliotek | [Angular Material 21](https://material.angular.io/) (Material Design 3) |
| Auth | [ASP.NET Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) med JWT |
| API | REST, [Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview) |

## Beroenden

### Backend (NuGet)

| Paket | Version | Syfte |
|---|---|---|
| [MediatR](https://github.com/jbogard/MediatR) | 14 | CQRS – kommando- och frågedistribution |
| [Microsoft.EntityFrameworkCore.SqlServer](https://learn.microsoft.com/en-us/ef/core/) | 9 | SQL Server-provider för EF Core |
| [Microsoft.AspNetCore.Identity.EntityFrameworkCore](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) | 9 | Användarhantering och lösenordshashning |
| [Microsoft.AspNetCore.Authentication.JwtBearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn) | 9 | JWT-validering |
| [Microsoft.AspNetCore.OpenApi](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi) | 9 | OpenAPI-dokumentation |

### Frontend (npm)

| Paket | Version | Syfte |
|---|---|---|
| [@angular/core](https://angular.dev/) | 21 | SPA-ramverk – standalone components, signals, control flow |
| [@angular/material](https://material.angular.io/) | 21 | UI-komponentbibliotek (Material Design 3) |
| [@angular/cdk](https://material.angular.io/cdk/categories) | 21 | Layouthjälpare, drag/drop, virtual scrolling |
| [rxjs](https://rxjs.dev/) | 7.8 | Reaktiva strömmar (minimal – signals prioriteras i templates) |
| [typescript](https://www.typescriptlang.org/) | 5.9 | Typsäker JavaScript (strict null checks) |

### Tester

| Paket | Version | Syfte |
|---|---|---|
| [xUnit](https://xunit.net/) | 2.9 | Testramverk för .NET (domän- och applikationstester) |
| [NSubstitute](https://nsubstitute.github.io/) | 5 | Mock-bibliotek för handlertester |
| [Testcontainers.MsSql](https://dotnet.testcontainers.org/) | 3 | SQL Server-container för integrationstester |
| [Vitest](https://vitest.dev/) | 4 | Testramverk för Angular/TypeScript |

### Designprinciper och arkitekturmönster

| Princip | Källa |
|---|---|
| [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) | Robert C. Martin – lager med beroende strikt inåt |
| [Domain-Driven Design (DDD)](https://www.domainlanguage.com/ddd/) | Eric Evans – aggregat, value objects, bounded contexts, domain events |
| [CQRS](https://martinfowler.com/bliki/CQRS.html) | Martin Fowler – separata modeller för läsning och skrivning |
| [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) | Martin Fowler – dataåtkomst bakom interface utan läckage av persistensteknik |
| [Conventional Commits](https://www.conventionalcommits.org/) | Strukturerade commit-meddelanden med type och scope |

## Kom igång

### Krav

| Verktyg | Version | Används till |
|---------|---------|--------------|
| [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) | 9.0 | Backend API |
| SQL Server | valfri lokal instans | Konventionsdatabas (dbo + identity-schema) |
| [Node.js](https://nodejs.org/) | 22+ | Angular frontend |
| [Angular CLI](https://angular.dev/tools/cli) | 21+ | Bygga och köra Angular-apparna |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | senaste | Integrationstester (SQL Server-container) |

Repo:t innehåller [global.json](global.json) och låser SDK till `.NET 9`.
Om `dotnet` klagar på "A compatible .NET SDK was not found" behöver du installera en 9.x-SDK lokalt.

#### Felsökning: saknad .NET 9 SDK

```powershell
winget install Microsoft.DotNet.SDK.9
dotnet --list-sdks
dotnet --version
```

Om installationen lyckats ska `dotnet --version` visa en 9.x-version när du kör kommandot i repo-roten.

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

#### Testa e-post lokalt med smtp4dev

För lokal utveckling kan du fånga upp alla utskick utan att skicka riktiga mail.

1. Installera smtp4dev (valfritt sätt):

```powershell
winget install rnwood.smtp4dev
```

eller:

```bash
docker run --rm -it -p 3000:80 -p 2525:25 rnwood/smtp4dev
```

2. Starta smtp4dev.
3. Lägg till eller uppdatera `Email` i `backend/src/ConventionSystem.Api/appsettings.Development.json`:

```json
{
  "Email": {
    "Provider": "Smtp",
    "FromName": "Konvent Dev",
    "FromEmail": "noreply@local.dev",
    "Smtp": {
      "Host": "localhost",
      "Port": 2525,
      "UseSsl": false,
      "UseStartTls": false,
      "Username": "",
      "Password": ""
    }
  }
}
```

4. Starta API:t och trigga ett flöde som skickar mail.
5. Öppna smtp4dev på `http://localhost:3000` och verifiera att meddelandet finns i inkorgen.

Tips:
- Om du kör smtp4dev som desktop-app kan SMTP-porten vara `25` i stället för `2525`.
- Behåll `Email.Provider = Smtp` i development, men använd `Logging` eller riktig leverantör i andra miljöer.

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

#### Frontendtester (Vitest)

```bash
# Kör frontendtester
cd frontend
npm run test
```

Kör tester per app när du vill begränsa körningen:

```bash
# Endast admin
cd frontend
npm run test -- admin

# Endast public
cd frontend
npm run test -- public
```

Tips: kör med watch-läge under utveckling för snabb återkoppling.

```bash
cd frontend
npm run test -- --watch
```

Watch-läge per app:

```bash
# Endast admin
cd frontend
npm run test -- admin --watch

# Endast public
cd frontend
npm run test -- public --watch
```

Testnivåer och minimikrav för frontend-PR:er beskrivs i
`docs/Frontend.md` under avsnittet "Frontendtester".

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
│   │   ├── ConventionSystem.Infrastructure/  # EF Core, repositories, identity, e-post
│   │   └── ConventionSystem.Api/             # Minimal API-endpoints, feed-endpoints
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
    ├── Backend.md      # Arkitekturprinciper och kodmönster per lager
    ├── Frontend.md     # Angular-konventioner och komponentmönster
    ├── UseCases.md     # Alla use cases med acceptanskriterier
    └── Roadmap.md      # Implementationsstatus och faser
```

Se `docs/Backend.md` för arkitekturprinciper, kodmönster och EF Core-regler.

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
| Aggregate roots | `VisitorRegistration`, `SessionRegistration`, `StaffApplication`, `Ticket`, `PromotionCode` |
| Entiteter | `Availability`, `StationPreference`, `TicketType`, `TicketPerk`, `PromotionCodeRedemption` |
| Value objects | `DiscountType` |
| Domain service | `RegistrationRuleService` (validerar platser, biljetter och promotionkoder) |

### Staff

Hanterar bemanningen av konventionen. Bemanningskoordinatorn skapar pass (`Shift`) på stationer, och funktionärer tilldelas via `StaffAssignment`. `AssignmentService` varnar vid överlappande pass men blockerar inte – koordinatorn har sista ordet. Staffansökan (i Registration-BC:n) är förutsättningen för att en person ska kunna tilldelas ett pass.

| Typ | Namn |
|---|---|
| Aggregate root | `Shift` |
| Entiteter | `StaffAssignment` |
| Value objects | `StaffingRequirement`, `TimeSlot` |
| Domain service | `AssignmentService` (kontrollerar överlapp – varning, blockerar inte) |
