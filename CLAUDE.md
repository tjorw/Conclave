# CLAUDE.md

Den här filen styr hur Claude Code arbetar i det här projektet.

## Språkkonvention

- **Kod och modellering:** Engelska – klassnamn, metoder, properties, variabler, namnrymder, databaskolumner
- **Dokumentation och resonemang:** Svenska – kommentarer, README, commit-meddelanden, svar i konversationen

## Teknikstack

- **Backend:** .NET 9, C# – Clean Architecture med DDD. Se `docs/Backend.md` för arkitekturprinciper och kodmönster.
- **ORM:** Entity Framework Core 9
- **Databas:** SQL Server (deploy-per-konvention – en databas per instans, `dbo`-schema för domändata, `identity`-schema för ASP.NET Identity)
- **Frontend:** Angular 21 (admin-app + publik vy) – standalone components, signals, reactive forms, Angular Material – se `docs/Frontend.md` för arkitekturprinciper
- **Auth:** ASP.NET Identity med JWT (stöd för OAuth planerat)
- **API:** REST, minimal API-endpoints

Se `README.md` för lösningsstruktur och domänmodell. Se `docs/Backend.md` för arkitekturprinciper, kodmönster per lager och EF Core-regler.

## Byggkommandon

### Backend

```bash
dotnet build backend/ConventionSystem.sln
dotnet test backend/ConventionSystem.sln
dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName~Convention"   # kör tester för ett specifikt bounded context
dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName!~Integration" # hoppa över integrationstester
dotnet run --project backend/src/ConventionSystem.Api
```

### Frontend

```bash
cd frontend
npm install          # en gång – installera beroenden

ng serve admin       # admin-app på http://localhost:4200
ng serve public      # publik app på http://localhost:4201
ng build             # bygg alla appar för produktion
ng test              # kör Vitest-tester
```

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

# Tester

## Regel: tester skrivs alltid tillsammans med koden

Varje gång kod ändras eller läggs till i domänlagret eller applikationslagret
**ska** motsvarande tester skrivas i samma arbetspass – inte efteråt.

- **Domänmetod tillagd eller ändrad** → enhetstester i `ConventionSystem.Domain.Tests`
  som täcker lyckligt flöde, invarianter och felfall.
- **Command handler tillagd eller ändrad** → handlertester i `ConventionSystem.Application.Tests`
  som täcker lyckligt flöde, felfall (entitet ej funnen, saknad behörighet) och att rätt
  repository-metoder anropas.

Tester körs alltid innan commit-förslag ges:
```bash
dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName~{BoundedContext}"
```

Se `docs/Backend.md` för testmönster (xUnit + NSubstitute, `Setup()`-hjälpare, stubbning av rätt
hämtningsmetod).

# Commit-strategi

## Grundregler
- Committa aldrig automatiskt. Fråga alltid användaren först.
- Committa aldrig halvfärdigt eller trasigt arbete.
- Varje commit ska representera en komplett, sammanhängande enhet.
- Efter varje commit ska relevant dokumentation granskas och vid behov uppdateras så att den inte glider isär från koden.

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
- `convention`, `event`, `registration`, `staff`, `team`
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
