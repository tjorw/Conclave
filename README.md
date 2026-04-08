# Conclave

System för att administrera, annonsera, registrera och driva hobbymässor (tabletop gaming) i Sverige.

## Teknikstack

- **Backend:** .NET 9, C#
- **Arkitektur:** Clean Architecture med DDD (Domain-Driven Design)
- **ORM:** Entity Framework Core
- **Databas:** SQL Server (multi-tenant)
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
src/
├── ConventionSystem.Domain/        # Domänlager – inga externa beroenden utom MediatR
├── ConventionSystem.Application/   # Use cases, commands, queries (CQRS)
├── ConventionSystem.Infrastructure/# EF Core, repositories, event dispatch
└── ConventionSystem.Api/           # Minimal API-endpoints

tests/
├── ConventionSystem.Domain.Tests/
└── ConventionSystem.Application.Tests/
```

Beroendet pekar alltid inåt: Infrastructure → Application → Domain.

## Domänmodell

Systemet är indelat i fyra bounded contexts:

| Context | Ansvar |
|---|---|
| **Convention** | Konvention, upplaga, person, lokal, station, kategori |
| **Event** | Evenemang med utkast/publicerad versionshantering, sessioner |
| **Registration** | Besökar-, sessions- och volontärregistrering, biljetter |
| **Volunteer** | Volontärpass, tilldelningar |

Contexts kommunicerar via domain events och id-referenser – ingen direkt koppling mellan aggregat.

## Multi-tenant

Varje konvention är en tenant med egen databas. En central systemdatabas hanterar tenant-registret och routing. En separat identitetsdatabas hanterar konton och autentisering.

## Domain events

Domain events dispatchar via MediatR efter lyckad `SaveChanges`. Skapa en handler genom att implementera `IDomainEventHandler<T>`:

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
