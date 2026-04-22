# Outbox-mönstret – asynkron kommunikation med externa tjänster

## Problem

Infrastrukturlagret kommunicerar med externa tjänster som SMTP. Om tjänsten är otillgänglig när ett HTTP-anrop hanteras misslyckas operationen – och domänändringen kan ha persisterats utan att t.ex. ett välkomstmejl skickades.

## Lösning: Outbox + bakgrundsjobb

Istället för att skicka direkt skriver applikationslagret en rad i `dbo.OutboxMessages` **i samma databastransaktion** som domänändringen. Ett bakgrundsjobb plockar upp obehandlade meddelanden och skickar dem.

Egenskaper:
- **Atomärt** – antingen sparas domänändringen *och* outbox-raden, eller inget alls.
- **Ingen datatap** – om SMTP är nere finns meddelandet kvar och skickas när tjänsten är uppe igen.
- **Ingen HttpContext-beroende** – bakgrundsjobbet läser enbart från databasen; all kontext (mottagare, tenant-id, payload) serialiserades in i raden vid skapandet under det ursprungliga HTTP-anropet.

## Datamodell

```
dbo.OutboxMessages
──────────────────
Id               uniqueidentifier   PK, default newsequentialid()
Type             nvarchar(100)      t.ex. "EmailMessage"
Payload          nvarchar(max)      JSON – självbärande, all kontext inkluderad
CreatedAt        datetimeoffset     sätts vid insättning
ProcessAfter     datetimeoffset     möjliggör retry-backoff (initialt = CreatedAt)
ProcessedAt      datetimeoffset?    null = ej behandlad
RetryCount       int                default 0
Error            nvarchar(max)?     senaste felmeddelande
```

### Payload-exempel för e-post

```json
{
  "to": "user@example.com",
  "subject": "Välkommen till Conclave",
  "htmlBody": "<p>…</p>",
  "tenantId": "a1b2c3d4-…"
}
```

Payload är intentionellt platt och självbärande. Bakgrundsjobbet deserialiserar och skickar utan att känna till HttpContext, claims eller tenant-resolution.

## Komponentöversikt

```
ApplikationsLager
  └── IEmailSender (interface)

InfrastrukturLager
  ├── OutboxEmailSender        implements IEmailSender
  │     skriver OutboxMessage-rad till databasen
  │
  ├── SmtpEmailSender          intern klass, används bara av processorn
  │     direktanrop mot SMTP-servern
  │
  └── OutboxProcessor          IHostedService
        kör var 30:e sekund
        hämtar obehandlade rader (ProcessedAt IS NULL AND ProcessAfter <= nu)
        skickar via SmtpEmailSender med Polly-retry
        markerar ProcessedAt vid lyckat svar
        ökar RetryCount + sätter ProcessAfter (exponentiell backoff) vid fel
```

## Retry-strategi (Polly)

Processorn använder Polly med exponentiell backoff för varje enskilt meddelande:

| Försök | Väntetid |
|--------|----------|
| 1      | omedelbart (i processorn) |
| 2      | 2 minuter (via `ProcessAfter`) |
| 3      | 8 minuter |
| 4      | 30 minuter |
| 5+     | 2 timmar |

Efter ett konfigurerbart maxantal försök markeras raden med ett slutgiltigt felmeddelande och hoppas över (dead-letter). En `Warning`-loggpost skrivs.

## Vad som INTE ska göras

- **Injicera `IHttpContextAccessor` i processorn** – HttpContext finns inte i bakgrundsjobb.
- **Skriva minimal payload och hämta resten dynamiskt** – outbox-raden måste vara självbärande; entiteter kan ha ändrats när processorn kör.
- **Skicka direkt från `OutboxEmailSender`** – hela poängen är den asynkrona avkopplingen.

## Viktiga designbeslut

- `OutboxEmailSender` (ej `SmtpEmailSender`) registreras som `IEmailSender` i DI-containern. Applagret känner aldrig till SMTP.
- `SmtpEmailSender` är `internal` och används bara av `OutboxProcessor`.
- Processorn körs som `IHostedService` och är fristående från request-pipeline.
- Processorn hämtar ett begränsat antal rader per körning (t.ex. 50) för att undvika minnesproblem vid uppbyggd kö.
