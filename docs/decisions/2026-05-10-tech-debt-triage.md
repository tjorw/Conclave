# ADR: Teknisk skuld – triagering och prioritering

**Datum:** 2026-05-10  
**Status:** Implementerad

---

## Kontext

Roadmap.md innehåller en lista med tio identifierade tekniska skulder. Inför produktionsstart behöver vi avgöra vad som faktiskt bör åtgärdas, i vilken ordning och vad som kan ligga kvar.

Analysen bygger på en genomgång av koden för varje punkt.

---

## Genomgång per skuld-post

### 1. E-postunikthet saknas på databasnivå — **ÅTGÄRDA** (hög prioritet)

Koden: `IX_persons_convention_id_email` är **inte** `.IsUnique()`. Unikthetskontroll sker bara på applikationsnivå via `EmailExistsInConventionAsync()` / `FindByEmailInConventionAsync()` — utan databas-constraint.

**Konsekvens:** Två parallella förstaloggar med samma e-postadress kan båda passera kontrollen och försöka infoga. Ingen `DbUpdateException`-hantering finns i login-flödet. Resultatet blir antingen en okontrollerad krasch eller en duplicerad person-rad.

**Koppling till skuld-posten "Idempotens i login-flödet":** Dessa är samma grundproblem. Löses ihop.

**Åtgärd:**
- Lägg `.IsUnique()` på `PersonConfiguration.Configure()` → migrering.
- Fånga `DbUpdateException` i login-flödet och returnera 409-svar med tydligt felmeddelande.
- Verifiera att `CreatePersonHandler` och `CreateWalkupPersonHandler` (de admin-skapade vägarna) hanterar konflikten med ett meningsfullt affärsfel i stället för en råa DbUpdateException.

---

### 2. Feed-endpoints saknar HTTP-caching — **ÅTGÄRDA** (medel prioritet)

Koden: Alla tre feed-endpoints (`/editions/{id}`, `/events/{id}`, `/active-edition`) returnerar `Results.Ok()` utan `Cache-Control`-header. Varje anrop gör en ny DB-runda.

**Konsekvens:** Feed-data är skrivskyddad och förändras sällan. Utan caching-headers kan varken CDN, proxyserver eller browser cacha svaren. Vid publik trafik (konventionsprogram på 200+ mobiler) ger det onödig DB-belastning.

**Åtgärd:**
- Sätt `Cache-Control: public, max-age=60` på edition- och event-feedsen.
- Sätt `Cache-Control: public, max-age=30` på `active-edition` (mer volatil).
- Lägg till `ETag` baserat på ett versionsfält eller hash om stöd för betingad request är önskvärt — men det är inte obligatoriskt för att lösa grundproblemet.
- Ingen infrastrukturändring krävs; det är en header på `Results.Ok()`-kallet.

---

### 3. `DbSet<Station>` och `DbSet<Venue>` saknas i ConventionDbContext — **ÅTGÄRDA** (låg prioritet, bundla)

Koden: `Station` och `Venue` nås via `db.Set<T>()` i minst 6 filer. Alla övriga entiteter har namngivna `DbSet<T>`-properties.

**Konsekvens:** Inget funktionellt problem idag, men inkonsistent och kräver extra kognitiv last vid varje ny query. En ovarsamhet leder lätt till `db.Set<Station>()` i stället för det förväntade `db.Stations`.

**Åtgärd:**
- Lägg till `public DbSet<Station> Stations => Set<Station>();` och `public DbSet<Venue> Venues => Set<Venue>();` i `ConventionDbContext`.
- Uppdatera de 6 filerna till att använda de namngivna properties.
- Ingen migration behövs (ingen schemaändring).

---

### 4. Cache stampede i `CachingTenantResolver` — **DEFERA**

Koden: `TryGetValue → miss → DB → Set` utan per-nyckel-samordning. Okända subdomains cachas inte alls.

**Konsekvens:** Vid burst-trafik mot en just-invaliderad tenant kan N parallella requests slå databasen. I praktiken handlar det om en konvention med en databas — belastningen är låg. Problemet är reellt men inte akut.

**Beslut:** Defera. Åtgärda med `SemaphoreSlim` per nyckel och kort negativ cache om och när mätbar DB-belastning uppstår.

---

### 5. Hemligheter i `appsettings.Development.json` — **DEFERA** (deployment-concern)

`Jwt:Key` i development-filen är gitignorerad och dokumenterad i README som ett lokalt mönster. Demo-deploy använder miljövariabler via `Run-DemoLocal.ps1`. Produktionsdeploy kräver Azure Key Vault eller liknande — men det är ett deployment-beslut, inte en kodfråga som ska lösas i repot.

**Beslut:** Defera. Dokumenteras i `docs/DemoDeploy.md` för den som sätter upp en riktig produktionsmiljö.

---

### 6. `Shift` saknar direkt `EditionId` — **DEFERA**

Koden fungerar korrekt via join mot `Edition.Stations`. Att lägga till `EditionId` på `Shift` kräver en migration och ändringar i `ShiftConfiguration` och berörda queries — för ett problem som inte existerar i nuläget.

**Beslut:** Defera. Åtgärda om och när Staff-kontexten växer med ytterligare direkta Shift-queries.

---

### 7. Deduplikering i tidsschema — **DEFERA**

Koden: Prioriteringslogiken `Booked > Organiser > Watching` i `MyScheduleRepository` är korrekt men otesterad på domännivå.

**Beslut:** Defera. Lägg till testtäckning om och när deduplikeringslogiken ändras.

---

### 8. `CreatePersonCommand` vs UC002 — **INGEN ÅTGÄRD** (löses av skuld 1)

Det finns fyra kodsökvägar för att skapa en person (`CreatePerson`, `CreateWalkupPerson`, `RegisterPerson` vid login, `RegisterPerson` vid registration). Alla kontrollerar e-post på applikationsnivå. Problemet är inte att det finns flera vägar — det är att ingen av dem backas av ett unikt databas-constraint. Skuld 1 (e-postunikthet) löser grundproblemet. Inga ytterligare åtgärder krävs.

---

### 9. Social inloggning (OAuth) — **UTANFÖR SCOPE**

Feature request, inte teknisk skuld. Tas upp separat om och när det efterfrågas.

---

### 10. E2E-tester för journeys — **UTANFÖR SCOPE**

Infrastrukturkostnad för Playwright/Cypress-installation motiveras inte förrän kritiska flöden är stabila och bemanning finns för underhållet.

---

## Beslut

Tre konkreta åtgärder genomförs:

| ID | Post | Prioritet |
|----|------|-----------|
| `R-TD01` | Unikt databas-constraint på `persons(convention_id, email)` + felhantering | Hög |
| `R-TD02` | `Cache-Control`-headers på feed-endpoints | Medel |
| `R-TD03` | `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` | Låg (bundla med R-TD01) |

Övriga sex poster deferas eller lämnas utanför scope med dokumenterat skäl.

---

## Motivering

**Varför R-TD01 nu?** Det är det enda felet med potentiell datakorruptionskonsekvens (duplikata persons). Det löser sig inte av sig självt och komplexiteten är låg.

**Varför R-TD02 nu?** Feed-caching är en rad per endpoint och kostar i princip ingenting att lägga till. Det är en av de saker som är enklast att glömma och svårast att motivera att prioritera efter lansering.

**Varför R-TD03 nu (bundlat med R-TD01)?** Inga schemaändringar krävs. Sex filuppdateringar och två rader i `ConventionDbContext`. Rätt tillfälle är nu när vi ändrar i infrastrukturkoden ändå.

**Varför defera resten?** Alla tre "defera"-poster fungerar korrekt idag. De representerar potentiella framtida problem vid skalsättning (cache stampede) eller domänexpansion (Shift.EditionId, schema-dedup). Att lösa dem nu innebär schemamigrationer och komplexitet utan omedelbar nytta.

---

## Bounded contexts och filer som påverkas

### R-TD01 — E-postunikthet

| Vad | Fil |
|-----|-----|
| Unikt index | `Infrastructure/Persistence/Configurations/Convention/PersonConfiguration.cs` |
| Migration | `AddUniqueEmailConstraintToPersons` |
| Felhantering login | `Api/Endpoints/AuthEndpoints.cs` |
| Felhantering admin | `Application/Convention/Commands/CreatePerson/CreatePersonHandler.cs` |
| Felhantering walkup | `Application/Convention/Commands/CreateWalkupPerson/CreateWalkupPersonHandler.cs` |

### R-TD02 — Feed-caching

| Vad | Fil |
|-----|-----|
| Cache-Control-headers | `Api/Endpoints/FeedEndpoints.cs` |

### R-TD03 — DbSet-konsekvens

| Vad | Fil |
|-----|-----|
| Lägg till DbSet-properties | `Infrastructure/Persistence/ConventionDbContext.cs` |
| Uppdatera db.Set<T>()-anrop | 6 repository-filer i `Infrastructure/Persistence/Repositories/` |

---

## Risker

| Risk | Sannolikhet | Åtgärd |
|------|-------------|--------|
| Unikt index slår mot befintliga duplikat i databasen | Låg (dev-data resettas) | Verifiera att inga duplikat finns innan migrering |
| `Cache-Control: public` cachas av proxy utan hänsyn till tenant | Låg (feed-data är publik) | Feeds är alltid publika; ingen tenant-specifik information |

---

## Acceptanskriterier

### R-TD01 — E-postunikthet

- [x] `persons`-tabellen har unikt constraint `UQ_persons_convention_id_email`
- [x] Försök att skapa en person med befintlig e-post ger `DuplicateEmailException` → 409 (ej raw DbUpdateException)
- [x] Login-flödet fångar `DuplicateEmailException` vid race condition och gör retry-lookup
- [ ] Integrationstesterna täcker att unik e-post upprätthålls *(kräver Docker – verifieras i CI)*
- [x] Befintliga unit tests är gröna (870/870)

### R-TD02 — Feed-caching

- [x] `GET /feed/{id}/editions/{editionId}` returnerar `Cache-Control: public, max-age=60`
- [x] `GET /feed/{id}/events/{eventId}` returnerar `Cache-Control: public, max-age=60`
- [x] `GET /feed/{id}/active-edition` returnerar `Cache-Control: public, max-age=30`
- [x] Befintliga tester är gröna

### R-TD03 — DbSet-konsekvens

- [x] `ConventionDbContext` har namngivna `DbSet<Station>` och `DbSet<Venue>`
- [x] Inga `db.Set<Station>()` eller `db.Set<Venue>()` kvar i codebasen
- [x] Bygget är grönt utan varningar
