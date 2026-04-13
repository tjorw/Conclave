# Backend-arkitektur

Dokumentet beskriver arkitekturprinciper, kodmönster och beslut för
backend-implementationen. Operationella instruktioner (byggkommandon,
commit-strategi) finns i `CLAUDE.md`.

---

## Lagerskikt

```
① API (Presentation)   – Minimal API-endpoints, middleware, auth
② Application          – Use cases: Commands, Queries, Handlers (CQRS)
③ Domain               – Aggregat, entiteter, value objects, domain events
④ Infrastructure       – EF Core, repositories, interceptors, identity
```

Beroenden pekar **alltid inåt**: Infrastructure beror på Domain, aldrig tvärtom.
Application-lagret definierar interface som Infrastructure implementerar.

---

## Domänlagret

### Aggregate roots

Ärver `AggregateRoot`. Privat parameterlös konstruktor för EF Core.

```csharp
public sealed class Edition : AggregateRoot
{
    private readonly List<Venue> _venues = [];

    public EditionId Id { get; private set; }
    public IReadOnlyList<Venue> Venues => _venues.AsReadOnly();

    private Edition() { }  // EF Core

    internal Edition(EditionId id, ...) { /* validera + sätt */ }

    public Venue CreateVenue(string name, string building, string? description = null)
    {
        var venue = new Venue(VenueId.New(), name, building, description);
        _venues.Add(venue);
        return venue;  // returnera för att handlern kan anropa MarkAsAdded
    }
}
```

**Regler:**
- Alla properties har `private set` – ingen extern mutation
- Publika samlingar exponeras som `IReadOnlyList<T>`; internt `List<T>`
- Invarianter kastas i konstruktorn – aldrig halvinitierat tillstånd
- Aggregat-rooten raisar domain events via `RaiseDomainEvent(...)`
- Metoder som skapar barn-entiteter **returnerar** den nya entiteten

### Entiteter

Ärver `Entity<TId>`. Konstruktor och muteringsmetoder märkta `internal` –
entiteter skapas och muteras bara inifrån sin aggregate root.

```csharp
public sealed class Venue : Entity<VenueId>
{
    public VenueId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    internal Venue(VenueId id, string name, string building, string? description)
    { /* validera + sätt */ }
}
```

### Value objects

Ärver `ValueObject`. Immutabla properties (`{ get; }` utan setter).
Implementerar `GetEqualityComponents()`.

**Monetära belopp** representeras som `int` (ören) eller `decimal` – aldrig `float`/`double`.

### Starka id-typer

`readonly record struct` med statisk `New()`. Genereras alltid i
applikationskod med `Guid.CreateVersion7()`.

```csharp
public readonly record struct VenueId(Guid Value)
{
    public static VenueId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
```

Konvertering till/från `Guid` sker i EF Core-konfigurationen, aldrig på
andra ställen.

### Domain events

`record` som implementerar `IDomainEvent`. Alltid namngivna i dåtid.
Innehåller alltid `OccurredAt`. Bär starka id-typer, aldrig råa `Guid`.

```csharp
public record EditionPublished(
    EditionId EditionId,
    PersonId PublishedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

Events har **två syften**:
1. Reaktiva triggers – andra bounded contexts lyssnar via `IDomainEventHandler`
2. Observerbara bevis i tester – `DomainEvents.OfType<X>().Single()`

---

## Applikationslagret

### Commands

`sealed record` som implementerar `IRequest<TResult>`. Bär råa `Guid` –
konvertering till starka id-typer sker i handlern.

```csharp
public sealed record CreateVenueCommand(
    Guid EditionId,
    string Name,
    string Building,
    string? Description) : IRequest<Guid>;
```

- Skapande returnerar `Guid` (det nya id:t)
- Mutationer returnerar ingenting (`IRequest` utan typparameter)
- Namngivna efter use case, inte CRUD: `PublishEdition`, inte `UpdateEditionStatus`

### Queries

`sealed record` som implementerar `IQuery<TResult>` (alias för
`IRequest<TResult>`). Returnerar DTO:er, aldrig domänobjekt.

### Handlers

`sealed class` med primär konstruktor. Ansvar i ordning:

1. Konvertera id:n till starka typer
2. Ladda aggregat med rätt includes (se repository-mönster nedan)
3. Ladda stödjande aggregat om behörighetscheck behövs
4. Kontrollera behörighet
5. Anropa domänmetod
6. `MarkAsAdded` om en ny barn-entitet skapades (se EF Core-fallgrop nedan)
7. `SaveAsync`

```csharp
public sealed class CreateVenueHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : IRequestHandler<CreateVenueCommand, Guid>
{
    public async Task<Guid> Handle(CreateVenueCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);

        var edition = await editionRepository.GetByIdWithStructureAsync(editionId, ct)
            ?? throw new InvalidOperationException("Upplaga hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new InvalidOperationException("Utföraren är inte administratör.");

        var venue = edition.CreateVenue(command.Name, command.Building, command.Description);
        editionRepository.MarkAsAdded(venue);   // se EF Core-fallgrop
        await editionRepository.SaveAsync(ct);

        return venue.Id.Value;
    }
}
```

Felmeddelanden kastas som `InvalidOperationException` (→ 422 via
`GlobalExceptionHandler`) vid regelbrott.
Behörighetsfel kastas som `UnauthorizedAccessException` (→ 401) – aldrig
`InvalidOperationException` för auth.

### Repository-interfaces

Definieras i `{BoundedContext}/Abstractions/`. En interface per
aggregate root. Metodsignaturer inkluderar alltid `CancellationToken ct = default`.

```csharp
public interface IEditionRepository
{
    Task AddAndSaveAsync(Edition edition, CancellationToken ct = default);
    Task<Edition?> GetByIdAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByIdWithStructureAsync(EditionId id, CancellationToken ct = default);
    void MarkAsAdded<T>(T entity) where T : class;
    Task SaveAsync(CancellationToken ct = default);
}
```

**Namnmönster för hämtningsmetoder:**
- `GetByIdAsync` – laddar aggregat utan collections (för läsning av skalärfält)
- `GetByIdWith{X}Async` – laddar aggregat med specifika collections inkluderade
- `GetProjectedByIdAsync` – projicerar direkt till DTO i databasen (LINQ Select)
- `ListBy{X}Async` – projicerar lista till DTO:er

### `ICurrentUser`

Injiceras i handlers som behöver den inloggades identity. Implementeras i
API-lagret via `HttpContext`.

```csharp
public interface ICurrentUser
{
    PersonId PersonId { get; }
}
```

**Begränsning:** `ICurrentUser` fungerar bara inom ett HTTP-request-scope.
Seeders, bakgrundsjobb och integrationstest-setup får **inte** anropa handlers
som injicerar `ICurrentUser` – de måste istället anropa domänmodellen direkt
och kalla `repository.MarkAsAdded(entity)` + `repository.SaveAsync()` manuellt.

### Domain event handlers

Implementerar `IDomainEventHandler<TEvent>`. Placeras i applikationslagret
under relevant bounded context. Korsande bounded context-reaktioner (t.ex.
`EditionPublished` → skapar Registration-kontext) implementeras här.

---

## Infrastrukturlagret

### EF Core-konfigurationer

En `IEntityTypeConfiguration<T>` per aggregat/entitet. Placeras under
`Persistence/Configurations/{BoundedContext}/`. Tabeller och kolumner
namnges med `snake_case`.

```csharp
public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(id => id.Value, value => new VenueId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
    }
}
```

**Regler:**
- Alla starka id-typer konverteras explicit med `HasConversion`
- Shadow properties används för FK:er som inte exponeras på domänklassen
- Collection-navigationer konfigureras med `.HasField("_venues")` för att
  mappa mot privata backing fields

### Indexstrategi

- Alla FK-kolumner som används i WHERE indexeras explicit
- Sammansatt index om tabell alltid filtreras på två kolumner tillsammans
- Statuskolumner och booleans indexeras **inte** – för låg selektivitet
- Namnformat: `IX_{tabell}_{kolumn}` eller `IX_{tabell}_{kolumn1}_{kolumn2}`

```csharp
builder.HasIndex(e => e.ConventionId)
    .HasDatabaseName("IX_editions_convention_id");
```

### Repositories

`sealed class` med primär konstruktor som tar `ConventionDbContext`.
`SaveAsync` anropas i slutet av varje skrivoperation – inget unit-of-work
exponeras utåt.

**`GetByIdWith*` – välj rätt metod:**
Välj den metod som laddar exakt de collections handlern behöver.
`GetByIdWithStructureAsync` är inte alltid rätt – ladda inte mer än nödvändigt.

### EF Core-fallgrop: nya barn-entiteter och `MarkAsAdded`

**Problem:** `HasDefaultValueSql("newsequentialid()")` markerar id-egenskapen
som `ValueGeneratedOnAdd`. När en ny barn-entitet läggs till via en laddad
collection-navigation (`_venues.Add(venue)`), identifierar EF Core det
icke-tomma Guid-värdet som "ej sentinel" och sätter initialt state till
`Unchanged`. Relationship fixup ändrar sedan till `Modified` → EF genererar
`UPDATE` på en rad som inte existerar → `DbUpdateConcurrencyException`.

**Lösning:** Anropa `editionRepository.MarkAsAdded(entity)` direkt efter
domänmetoden och innan `SaveAsync`. Implementationen sätter
`db.Entry(entity).State = EntityState.Added` explicit.

```csharp
var venue = edition.CreateVenue(command.Name, command.Building, command.Description);
editionRepository.MarkAsAdded(venue);   // måste kallas för nya barn-entiteter
await editionRepository.SaveAsync(ct);
```

**Gäller för:** alla `Create*`-handlers som skapar barn-entiteter under ett
aggregat (Venue, StaffArea, Station, Category och framtida liknande typer).
Gäller **inte** för aggregat-roten själv (den läggs till med `AddAndSaveAsync`).

### `EventDispatchInterceptor`

Körs automatiskt vid `SaveChanges`. Hittar domain events på aggregate roots
i change trackern, serialiserar dem till `domain_event_log`-tabellen i samma
transaktion, och dispatchar sedan via MediatR.

Domain event handlers behöver **inte** anropa `SaveAsync` på nytt –
interceptorn sköter det.

---

## API-lagret

### Endpoints

Statiska klasser med en `Map*Endpoints`-extensionmetod. Registreras i
`Program.cs`. Request-typer definieras som `record` i samma fil.

```csharp
public static class EditionEndpoints
{
    public static IEndpointRouteBuilder MapEditionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/editions/{editionId:guid}/venues",
            async (Guid editionId, CreateVenueRequest req, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateVenueCommand(
                    editionId, req.Name, req.Building, req.Description), ct);
                return Results.Created($"/venues/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        return app;
    }
}

record CreateVenueRequest(string Name, string Building, string? Description);
```

**URL-mönster:**
- Skapande under förälder: `POST /conventions/{id}/editions`
- Mutation på känd resurs: `PUT /editions/{id}/categories/{catId}`
- Returkoder: `201 Created` med location-header vid skapande, `204 No Content` vid mutation

### Behörighet

Tre nivåer:

| Nivå | Dekorering | Används för |
|------|-----------|-------------|
| Admin | `.RequireAuthorization("IsAdmin")` | Konventionsstruktur, publicering, personhantering |
| Autentiserad | `.RequireAuthorization()` | Arrangörsflöden, staffansökningar, registrering |
| Publik | (ingen) | Alla GET-queries |

Domänspecifika rollkontroller (arrangör, kategoriansvarig etc.) görs i
handlern, inte i endpointen.

### Felhantering

`GlobalExceptionHandler` mappar undantag till `ProblemDetails`:

| Undantag | HTTP-status |
|----------|-------------|
| `InvalidOperationException` | 422 Unprocessable Entity |
| `ArgumentException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| Övrigt | 500 Internal Server Error |

`detail`-fältet i `ProblemDetails` innehåller svenska felmeddelandet.
Frontenden extraherar detta med `err?.error?.detail`.

---

## Tester

### Domäntester

Testar aggregate root-beteende direkt – inga mockar, ingen infrastruktur.

```csharp
[Fact]
public void Publish_WhenAlreadyPublished_Throws()
{
    var edition = /* setup */;
    edition.Publish(adminId);
    edition.ClearDomainEvents();  // isolera events från setup-stegen

    Assert.Throws<InvalidOperationException>(() => edition.Publish(adminId));
}

[Fact]
public void Publish_RaisesEditionPublishedEvent()
{
    var edition = /* setup */;
    edition.ClearDomainEvents();

    edition.Publish(adminId);

    var evt = edition.DomainEvents.OfType<EditionPublished>().Single();
    Assert.Equal(edition.Id, evt.EditionId);
}
```

`ClearDomainEvents()` anropas alltid efter setup-steg för att isolera events
från det specifika anropet under test.

### Applikationstester (handlertester)

Handlers testas med NSubstitute-mockade repositories. Aggregat skapas direkt
(ingen mock) – domänlogiken är riktig, bara persistensen mockas. En testklass
per handler.

**Viktigt:** Stuba rätt hämtningsmetod. Om handlern kallar
`GetByIdWithCategoriesAsync`, stuba den – inte `GetByIdAsync`.

```csharp
// Fel – handlern kallar GetByIdWithCategoriesAsync
_editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

// Rätt
_editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
```

`MarkAsAdded<T>` är void och stubbas automatiskt av NSubstitute som no-op.

---

## Domänspecifika implementationsdetaljer

Regler som inte framgår direkt av koden och kräver aktiv uppmärksamhet vid implementering.

### Event BC

- Innehållsfälten (`Title`, `Description`, `RegistrationType`, `DropInRules`) och `SessionRequest`-samlingen ägs direkt av `Event`-aggregatet och är redigerbara när `Status == EventStatus.Draft`.
- `SessionRequest` har ingen koppling till `Session`. Kategoriansvarig äger schemat och schemalägger sessioner oberoende av arrangörens önskemål – det är ett medvetet designbeslut, inte ett fel.

### Staff BC

- `AssignmentService` varnar vid överlappande pass men blockerar inte. Det finns ingen invariant
  i domänen – bemanningskoordinatorn har sista ordet. Lägg inte till en `throw` här.

---

## Bounded contexts och kommunikation

Contexts kommunicerar via domain events och id-referenser – aldrig via
direkt aggregatreferens.

```
Convention ──EditionPublished──▶ Event (skapar evenemangskontext)
Convention ──EditionPublished──▶ Registration (öppnar registreringskontext)
Event      ──EventCancelled───▶ Registration (avbokar sessionsregistreringar)
Staff      ──ShiftCancelled───▶ Staff (avbokar tilldelningar)
```

Cross-context id-referenser (t.ex. `CategoryId` i Event-domänen) lagras som
råa `Guid` eller en lokal wrapper – **aldrig** som en navigation property
till det andra contextets entitet.
