# CLAUDE.md

Den här filen styr hur Claude Code arbetar i det här projektet.

## Språkkonvention

- **Kod och modellering:** Engelska – klassnamn, metoder, properties, variabler, namnrymder, databaskolumner
- **Dokumentation och resonemang:** Svenska – kommentarer, README, commit-meddelanden, svar i konversationen

## Teknikstack

- **Backend:** .NET 9, C# – Clean Architecture med DDD. Se `docs/Backend.md` för arkitekturprinciper och kodmönster.
- **ORM:** Entity Framework Core
- **Databas:** SQL Server (multi-tenant – en databas per konvention + systemdatabas + identitetsdatabas)
- **Frontend:** Angular (separata appar för admin och publik vy) – se `docs/Frontend.md` för arkitekturprinciper
- **Auth:** ASP.NET Identity med stöd för social inloggning (OAuth)
- **API:** REST, minimal API-endpoints

## Byggkommandon

```bash
dotnet build backend/ConventionSystem.sln
dotnet test backend/ConventionSystem.sln
dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName~Convention"   # kör tester för ett specifikt bounded context
dotnet run --project backend/src/ConventionSystem.Api
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
│   │   └── ConventionSystem.Api/             # Controllers, minimal API-endpoints, feed-endpoints
│   └── tests/
│       ├── ConventionSystem.Domain.Tests/
│       ├── ConventionSystem.Application.Tests/
│       └── ConventionSystem.Integration.Tests/
├── frontend/
│   └── projects/
│       ├── admin/     # Admin-app (Angular Material)
│       ├── public/    # Publik vy (Angular Material)
│       └── shared/    # Delat bibliotek: API-typer, auth, interceptors
└── docs/
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

## Domänmodell

Se `README.md` för en komplett översikt av aggregate roots, entiteter och value objects per bounded context. Se `docs/Backend.md` för domänspecifika implementationsdetaljer och EF Core-konfigurationsregler.

# Kodkonventioner

## Allmänt (alla lager)

- Alla klasser är `sealed` om de inte är basklasser
- `file-scoped namespaces` (`namespace X.Y;`)
- Primära konstruktorer för dependency injection (`sealed class Foo(IBar bar)`)
- Felmeddelanden på svenska
- `DateTimeOffset.UtcNow` – aldrig `DateTime.Now`
- `async/await` med `CancellationToken ct` som sista parameter, alltid med defaultvärde `= default` i interface-signaturer

Se `docs/Backend.md` för fullständiga kodkonventioner per lager (domän, applikation, infrastruktur, API), auktoriseringsmodell, testmönster och kända EF Core-fallgropar.
Se `docs/Frontend.md` för Angular-konventioner.

# Commit-strategi

## Grundregler
- Committa aldrig automatiskt. Fråga alltid användaren först.
- Committa aldrig halvfärdigt eller trasigt arbete.
- Varje commit ska representera en komplett, sammanhängande enhet.

## När ska vi fråga om commit
Fråga "Redo att committa? Förslag: [meddelande]" när:
- Ett komplett use case är implementerat (domän, applikation, infrastruktur, API och tester)
- En fristående refaktorering är klar
- En strukturell förändring är klar (t.ex. lösningsuppsättning, mappstruktur)

Fråga inte om commit efter:
- Att bara en del av ett use case är implementerat
- Att en enskild klass eller fil lagts till som ännu inte är användbar
- En förändring som användaren inte bekräftat är godkänd

## Format på commit-meddelanden
Använd conventional commits:

```
<type>(<scope>): <kort beskrivning på engelska>

[valfri brödtext på svenska som förklarar varför, inte vad]
```

**Typer:**
- `feat` – ny funktionalitet
- `fix` – buggfix
- `refactor` – omstrukturering utan beteendeförändring
- `test` – lägger till eller uppdaterar tester
- `docs` – endast dokumentation
- `chore` – verktyg, beroenden, konfiguration

**Scope** motsvarar bounded context eller lager:
- `convention`, `event`, `registration`, `staff`
- `infrastructure`, `api`, `domain`

**Exempel:**
```
feat(convention): implement UC001 create convention
feat(event): implement UC-EV003 submit event for review
test(convention): add unit tests for Edition.Publish invariants
refactor(domain): extract TimeSlot value object to shared kernel
```

## Vad som hör till en commit
En use case-commit ska innehålla:
- Domänändringar (aggregatmetoder, domain events, value objects)
- Applikationslagret (command, command handler, validator)
- Infrastrukturförändringar (EF Core-konfiguration, migrationer om tillämpligt)
- API-endpoint
- Enhetstester för domän- och applikationslagret
- Acceptanskriterier i `docs/UseCases.md` markerade som klara (`[ ]` → `[x]`)
- `README.md` uppdaterad om domänmodellen har förändrats (nya aggregat, entiteter, value objects eller viktiga regler)

## Vad som aldrig ska vara i en commit
- Flera orelaterade use cases
- Utkommenterad kod
- Trasiga tester
- TODO-kommentarer som pekar på oimplementerat obligatoriskt beteende
