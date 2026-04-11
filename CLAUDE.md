# CLAUDE.md

Den här filen styr hur Claude Code arbetar i det här projektet.

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

## Domänmodell

Se `README.md` för en komplett översikt av aggregate roots, entiteter och value objects per bounded context.

**Viktiga implementationsdetaljer:**
- `Event.publishedVersionId` och `draftVersionId` är nullable FK:er med cirkulär referens mot `EventVersion` – konfigurera med `IsRequired(false)` och `OnDelete(DeleteBehavior.NoAction)` i EF Core
- `SessionRequest` har ingen koppling till `Session` – kategoriansvarig äger schemat och behöver inte följa requests
- `AssignmentService` varnar vid överlappande pass men blockerar inte – ingen invariant i domänen

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

### Indexstrategi
Varje tabell ska ha index på alla FK-kolumner som används i WHERE-filter. Index läggs alltid explicit i konfigurationsklassen med `HasDatabaseName` för tydliga namn.

**Regler:**
- Alla FK-kolumner indexeras – EF skapar bara automatiska index när `HasForeignKey` är konfigurerat, övriga måste läggas manuellt
- Om en tabell alltid filtreras på två kolumner tillsammans (t.ex. `convention_id + Email`) används ett sammansatt index istället för två separata
- Statuskolumner och boolean-flaggor indexeras **inte** – för låg selektivitet
- Namnformat: `IX_{tabell}_{kolumn}` eller `IX_{tabell}_{kolumn1}_{kolumn2}`

```csharp
// Enkelt FK-index
builder.HasIndex(e => e.ConventionId).HasDatabaseName("IX_editions_convention_id");

// Sammansatt index – filtreras alltid på båda
builder.HasIndex(p => new { p.ConventionId, p.Email }).HasDatabaseName("IX_persons_convention_id_email");
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

## Auktorisering

Systemet använder en tvånivåmodell: JWT-claims för statiska konventionsroller och inline domänkontroller i handlers för ägarskapsberoende roller.

### JWT-claims

| Claim | Typ | Innebörd |
|-------|-----|----------|
| `person_id` | `Guid` | Den inloggades PersonId i konventionen – alltid med |
| `is_admin` | `"true"` | Personen är `ConventionAdministrator` i konventionen – läggs bara till om sant |

Claims utfärdas vid login baserat på domäntillståndet vid den tidpunkten. Om en person läggs till som admin måste de logga in på nytt för att få `is_admin`-claim.

### Skydd av endpoints

**Adminendpoints** – kräver `is_admin`-claim:
```csharp
app.MapPost("/editions/{id}/publish", ...).RequireAuthorization("IsAdmin");
```
Används för: konventionsstruktur (upplaga, lokal, kategori, funktionsområde, station), publicering, kopiering av struktur, hantering av administratörer, direktskapande/uppdatering/avaktivering av personer.

**Autentiserade endpoints utan rollkrav** – kräver bara giltig token:
```csharp
app.MapPost("/events/{id}/submit", ...).RequireAuthorization();
```
Används för: arrangörsflöden (skapa/redigera evenemang, skicka in för granskning), staffansökningar, besöksregistrering, tilldelningar. Eventuella ägarskapskontroller görs i handlern (se nedan).

**Publika endpoints** – ingen autentisering:
```csharp
app.MapGet("/editions/{id}", ...); // ingen .RequireAuthorization()
```
Används för: alla GET-queries.

### Domänkontroller i handlers

Roller som härleds ur domäntillståndet (arrangör, kategoriansvarig, bemanningskoordinator etc.) kontrolleras **inte** i endpointen utan i handlern, nära affärslogiken:

```csharp
// I en handler – kontrollera ägarskap innan domänmetod anropas
if (eventAggregate.LeadOrganiserId != currentUser.PersonId)
    throw new UnauthorizedAccessException("Bara huvudarrangören kan utföra denna åtgärd.");
```

`UnauthorizedAccessException` → 401 via `GlobalExceptionHandler`. Använd **inte** `InvalidOperationException` för behörighetsfel – det ger 422 och döljer att det är ett auktoriseringsproblem.

### Vad som inte ska göras

- Lägg inte till JWT-claims för domänspecifika roller (`is_event_coordinator`, `is_category_responsible` etc.) – de beror på domäntillståndet och skulle bli inaktuella
- Gör inte domänkontroller i endpointen – handlers har bättre kontext och aggregatet är redan laddat

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
