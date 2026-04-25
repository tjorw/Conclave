# Dataunderhall och retention

Dokumentet beskriver vilka tabeller som kan underhallas av bakgrundsjobb och
vilka retention-regler som bor galla. Syftet ar att halla driftdata liten,
minska exponering av personuppgifter och undvika att gamla tekniska loggar
vaxer utan kontroll.

Reglerna nedan ar forslag for forsta produktionsversionen. De ska behandlas
som konservativa standardvarden tills verksamheten har beslutat om langre
lagringskrav for revision, ekonomi eller statistik.

---

## Principer

- Borja med infrastrukturdata, inte affarsdata. `outbox_messages` och
  `domain_event_log` ar lag risk jamfort med registreringar, biljetter och
  personalansokningar.
- Radera bara rader som inte langre kan paverka anvandarfloden. Ett
  outbox-meddelande som inte ar skickat far inte tas bort om det fortfarande
  kan processas.
- Anonymisera hellre an radera nar raden ingar i statistik, ekonomi eller
  historik.
- Kor i sma batcher, till exempel 500 rader per omgang, sa att jobbet inte
  laser stora delar av databasen.
- Logga antal borttagna/anonymiserade rader per tabell och retention-regel.
- Bakgrundsjobb far inte bero pa `ICurrentUser` eller `HttpContext`.

---

## Rekommenderad forsta implementation

### `OutboxCleanupJob`

Kor en gang per dygn.

Regler:

- Ta bort skickade outbox-meddelanden dar `processed_at` inte ar `null` och
  `processed_at < now - 30 dagar`.
- Ta bort permanent misslyckade meddelanden efter 90 dagar, men bara om de
  tydligt ar parkerade och inte langre ska provas igen. I nuvarande modell kan
  det vara meddelanden dar `processed_at is null` och `process_after` ar satt
  langt fram, till exempel `DateTimeOffset.MaxValue`.
- Ror aldrig meddelanden dar `processed_at is null` och `process_after <= now`.
  De ar aktiva och ska skickas av `OutboxProcessor`.

Motivering:

`outbox_messages.payload` innehaller e-postadress och mailinnehall. Skickade
mail behovs normalt bara kort tid for felsokning och ska inte ligga kvar som
permanent historik.

### `DomainEventLogCleanupJob`

Kor en gang per dygn eller vecka.

Regler:

- Om `domain_event_log` endast anvands for felsokning: ta bort rader dar
  `occurred_at < now - 180 dagar`.
- Om loggen ska anvandas som revisionslogg: behall langre enligt beslutad
  retention, till exempel 2-7 ar, eller undanta vissa `event_type`.

Motivering:

`domain_event_log.payload` ar JSON och kan vaxa snabbt. Tabellen ar
infrastrukturhistorik, men vissa event kan samtidigt vara revisionsrelevanta.
Beslutet om retention ska darfor vara explicit innan jobbet aktiveras i
produktion.

---

## Kandidater for senare fas

### `session_watches`

Regel:

- Ta bort bevakningar for upplagor vars slutdatum ar aldre an 30-90 dagar.

Motivering:

Sessionsbevakningar ar anvandarpreferenser for ett aktuellt schema. Efter att
upplagan ar avslutad har de normalt inget affarsvarde.

### `co_organiser_applications`

Regel:

- Anonymisera eller ta bort `Rejected` och `Cancelled` efter 180 dagar fran
  `reviewed_at` eller `requested_at`.
- Behall `Approved`, eftersom den ar kopplad till faktisk arrangorshistorik.

Motivering:

Raderna innehaller e-postadress och eventuell fritext. Slutbehandlade avslag
och aterkallade forslag bor inte sparas langre an nodvandigt.

### `event_comments`

Regel:

- Behall kommentarer som fortfarande kraver hantering.
- Anonymisera eller ta bort `Acknowledged` efter 1 ar efter upplagans slut.

Motivering:

Kommentarer kan vara arbetsflodeshistorik mellan arrangor och ansvariga.
Fardigbehandlade kommentarer kan dock innehalla fritext och bor omfattas av
retention.

---

## Affarsdata: anonymisera fore radering

Foljande entiteter ska normalt inte hard-raderas av ett generellt stadjobb:

- `VisitorRegistration`
- `Ticket`
- `StaffApplication`
- `PromotionCode` och `PromotionCodeRedemption`

Regler:

- Betalda och insamlade biljetter ska behallas sa lange ekonomi, support och
  statistik kraver det.
- Avbrutna reservationer utan betalningsreferens kan tas bort efter 30-90 dagar
  om de inte behovs for rapportering.
- Avslagna personalansokningar kan anonymiseras efter 1 ar efter upplagans slut.
- Kampanjkoder och inlosenhistorik ska behallas sa lange de behovs for
  uppfoljning av kampanjer och biljettintakter.

Motivering:

Dessa rader ar en del av konventets affarsfloden. Ett generellt
bakgrundsjobb ska inte forstora ekonomisk historik, deltagarstatistik eller
supportunderlag.

---

## Implementation

Bakgrundsjobbet bor ligga i `ConventionSystem.Infrastructure`, pa samma niva
som `OutboxProcessor`.

Rekommenderat monster:

- En `BackgroundService` eller schemalagd hosted service for dataunderhall.
- Konfigurerbara retention-varden i `appsettings`, till exempel:

```json
{
  "DataMaintenance": {
    "Enabled": true,
    "OutboxProcessedRetentionDays": 30,
    "OutboxFailedRetentionDays": 90,
    "DomainEventLogRetentionDays": 180,
    "BatchSize": 500
  }
}
```

- En korning per dygn racker for forsta versionen.
- Varje tabell stadar i egen batch och sparar separat, sa att ett fel inte
  stoppar alla andra regler.
- Jobbet ska kunna stangas av helt med `DataMaintenance:Enabled = false`.

---

## Roadmap

Forsta steget bor vara:

- `R-DM01` Implementera `OutboxCleanupJob` och `DomainEventLogCleanupJob` med
  konfigurerbar retention.

Senare steg:

- `R-DM02` Anonymisering av slutbehandlade medarrangorsansokningar.
- `R-DM03` Stadning av gamla sessionsbevakningar efter avslutad upplaga.
- `R-DM04` Beslut och implementation for anonymisering av registrerings- och
  personaldata.
