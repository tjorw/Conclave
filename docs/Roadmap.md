# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – återstående arbete, högst prioritet överst.

- [ ] `R-SCH03` Datumkontroller i boknings-, pass- och sessionsflöden föreslår första konventsdagen och dagens standardtider där det passar användarflödet.

### Laganmälningar (R-TM)

- [ ] `R-TM01` `Event.RegistrationMode: Individual | Team` + `TeamSize { Min, Max }` – arrangören konfigurerar anmälningsläge och lagstorlek per evenemang; se UC-TM001
- [ ] `R-TM02` `Team`-aggregat – Edition-scoped, captain (`PersonId`), lagnamn (obligatoriskt); `Members[]` valfritt och ej obligatoriskt i fas 1; se UC-TM002
- [ ] `R-TM03` `TeamEventRegistration`-aggregat – lag anmäler sig till evenemang, livscykel `Pending → Confirmed | Cancelled`; se UC-TM002, UC-TM003, UC-TM004
- [ ] `R-TM04` Admin-vy: arrangör tilldelar lag till session (`TeamSessionAssignment` på `Session`)
- [ ] `R-TM05` Tidschema: lagmedlemmars tilldelade sessioner visas via query-projektion (utökning av `MyScheduleRepository`)
- [ ] `R-TM06` Publik vy: laganmälningsflöde – captain anmäler lag och anger lagnamn; lagmedlemmar behöver inte anges i fas 1


### Bokning och tilldelning av plats
Platser i arrangemang kan tilldelas på olika sätt. Kön hör till det konkreta objektet man anmäler sig till; i nuvarande modell är det en session. Arrangemanget äger reglerna för hur sessionernas bokningar hanteras, till exempel om första bokningsförsöket skall bekräftas direkt eller hamna i kö/väntlista, och om samma person får boka flera sessioner i samma arrangemang.
- [ ] `R-BK01` Bokningskö – första bokningsförsök skapar en väntande bokning på den aktuella sessionen när arrangemanget kräver tilldelning i stället för direkt bekräftelse
- [ ] `R-BK02` Bokningstilldelning – stöd strategi per arrangemang för tilldelning av sessionernas väntande bokningar: först till kvarn, lottning eller manuell tilldelning


### Multitenancy

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [ ] `R-MT014` `portal`-app: self-service signup (publik del)
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

### Rikt innehåll (R-RC)

Se `docs/RichContent.md` för arkitektur och designbeslut. Use cases: UC-RC001–UC-RC006 i `docs/UseCases.md`.

Implementationsordning: R-RC01 → R-RC03 → R-RC02 → R-RC04

- [x] `R-RC01` Markdown i eventbeskrivningar – `Description`-fältet (max 10 000 tecken) stödjer markdown; live preview i admin-editorn; publik vy renderar med `ngx-markdown` (UC-RC001)
- [x] `R-RC02` Bilduppladdning – `IFileStorage`-abstraktion; `LocalDiskFileStorage` (MVP) + `BlobFileStorage` (stub); endpoint `POST /api/uploads`; bilder refereras via URL i markdown (UC-RC002)
- [x] `R-RC03` Redaktionella informationssidor – `Page`-aggregat i nytt `Content` bounded context; konventions- eller upplagescopead; `IsPublished`-flagga; admin CRUD + publik `GET /api/pages/{slug}` (UC-RC003, UC-RC004)
- [ ] `R-RC04` Mailmallar – adminredigerbara mallar i databas; standardmall per typ i kod (restore-funktion); `TemplateRenderer` med Markdig + variabelsubstitution; 

### CMS och innehållsstyrning (R-CMS)

All text och allt innehåll som visas i publika appen ska kunna styras från admin utan kodändringar. Se UC-CMS001–UC-CMS004 i `docs/UseCases.md`.

Implementationsordning: R-CMS01 → R-CMS02 → R-CMS03 → R-CMS04

- [x] `R-CMS01` `EditionContent`-entitet (EditionId, Key, Value) – nyckel-värde-par för startsidans texter (hero-rubrik, ingress, CTA-etiketter); admin-UI + publik konsumtion med fallback (UC-CMS001)
- [x] `R-CMS02` `Event.IsFeatured` + `FeaturedSortOrder` – admin väljer utvalda evenemang; publik startsida konsumerar `/api/events/featured` med fallback till tre senast publicerade (UC-CMS002)
- [ ] `R-CMS03` `Page.MenuSortOrder` – admin styr ordningen på menysidor; publik navigation sorterar stigande på ordningstalet (UC-CMS003)
- [ ] `R-CMS04` Publik startsida konsumerar `EditionContent`, utvalda evenemang och sorterad meny i ett sammanhängande flöde; inga hårdkodade texter i klientkoden (UC-CMS004)

#### Adminscope och pages

Adminytan ska skilja på konventionsnivå och upplagenivå. En vy på konventionsnivå får inte bero på vald upplaga i topbaren. En vy på upplagenivå ska alltid ligga under vald upplaga i navigationen och routas med `editions/:id/...`.

**Konventionsnivå**
- Dashboard och upplageöversikt.
- Personregister när listan avser hela konventets personbas.
- Feeds, om feeden inte har upplagebunden data.
- Informationssidor med `Page.EditionId == null`, routade som `/pages`.

**Upplagenivå**
- Grunduppgifter, livscykel, lokaler, kategorier, programtaggar, biljettyper, innehållsinställningar och export.
- Arrangörer, evenemang, schemaläggning, besökare, funktionärer, reception, biljetter, kampanjkoder och funktioneringsschema.
- Informationssidor med `Page.EditionId == :id`, routade som `/editions/:id/pages`.

**Implementationssteg**
1. `R-RC03.1` Dela admin-pages i två listvyer: `/pages` listar bara konventionssidor och `/editions/:id/pages` listar bara sidor för vald upplaga.
2. `R-RC03.2` Gör page-detaljvyn scope-styrd av route. Ta bort fri scope-väljare i formuläret. `/pages/new` skapar `editionId: null`; `/editions/:id/pages/new` skapar `editionId: :id`.
3. `R-RC03.3` Utöka `ListPagesQuery` och `IPageRepository.ListAsync` med exakt scope-filter (`editionId == null` eller `editionId == :id`). Slug-unikhet fortsätter gälla per scope.
4. `R-RC03.4` Lägg till frontend-validering i detaljvyn: en konventionsroute får bara visa sidor utan `editionId`; en editionroute får bara visa sidor vars `editionId` matchar route-parametern.
5. `R-CMS03.1` Lägg till `Page.MenuSortOrder` efter scope-delningen, så sortering kan hanteras separat för konventionsmeny och upplagemeny.
6. `R-CMS04.1` Låt publik navigation fortsätta prioritera aktiv upplagas sida framför konventionssida med samma slug, men sortera menyresultatet med `MenuSortOrder` inom det slutliga scope-valet.
7. `R-ADM01` Flytta övriga upplageberoende adminvyer från top-level routes till `editions/:id/...`. Inga redirects behövs innan produktionssättning; gamla top-level routes tas bort.

### Varumärke per konvent (R-BR)

Publika appen ska reflektera respektive konvents grafiska profil utan redeploy. Se UC-BR001–UC-BR002 i `docs/UseCases.md`.

Implementationsordning: R-BR01 → R-BR02

- [ ] `R-BR01` `ConventionBranding`-entitet (ConventionId, PrimaryColor, AccentColor, LogoUrl, FaviconUrl, FontFamily, CustomCss) – upsert-semantik; endpoint `PUT /api/conventions/{id}/branding`; anonym `GET`-endpoint med `Cache-Control: max-age=300`; admin-UI med färgväljare, filuppladdning och typsnittsval (UC-BR001)
- [ ] `R-BR02` Publik shell hämtar branding vid initialisering och applicerar CSS-variabler via `document.documentElement.style.setProperty`; logotyp sätts i navbar; fallback till systemdefinierade värden om anropet misslyckas (UC-BR002)

### Flerspråksstöd (R-I18N)

Stöd för att redigera och visa innehåll på flera språk. Implementeras i fas efter CMS och Varumärke. Se UC-I18N001–UC-I18N004 i `docs/UseCases.md`.

Implementationsordning: R-I18N01 → R-I18N02 → R-I18N03 → R-I18N04 → R-I18N05

- [ ] `R-I18N01` Språkstyrning – samla kvarvarande hårdkodade UI-texter bakom labels/översättningslager och förbered engelsk version
- [ ] `R-I18N02` `EditionLocale`-entitet (EditionId, Locale, IsPrimary) – admin aktiverar språk per upplaga; primärspråk alltid exakt ett (UC-I18N001)
- [ ] `R-I18N03` `PageTranslation`-entitet (PageId, Locale, Title, Content) – admin redigerar översättningar via flikbaserat UI; `GetPageBySlugQuery` utökas med locale-parameter och fallback (UC-I18N002)
- [ ] `R-I18N04` `EventTranslation`-entitet (EventId, Locale, Title, Description) – arrangör/admin redigerar översättningar; publika eventqueries tar emot locale-parameter (UC-I18N003)
- [ ] `R-I18N05` Publik språkväljare-komponent + locale-signal-service; locale skickas som query-parameter; `localStorage`-persistens; `Accept-Language`-fallback (UC-I18N004)

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| **Cache stampede i `CachingTenantResolver`** | Mönstret `TryGetValue → miss → DB → Set` utan per-nyckel-samordning kan ge N parallella DB-träffar vid burst mot samma tenant efter TTL/invalidering. Okända tenants cachas inte alls, så upprepade requests mot okänd subdomain fortsätter slå DB. Lös med per-key `SemaphoreSlim`/single-flight och överväg kort negativ cache för okända subdomains. Låg risk vid nuvarande skala. | Låg |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| **Feed-cachning och API-nyckel** | Feed-endpointsen är öppna och läser från databasen vid varje anrop. Vid hög trafik bör svaren cachas (HTTP-headers `Cache-Control`/`ETag`, CDN-lager eller Redis). Vid behov av skyddade feeds kan en API-nyckel läggas till utan att ändra URL-strukturen. | Medel – utvärdera inför produktion |
| **E2E-test för journeys** | Journey-flöden saknar UI-verifiering över hela kedjan. Lägg till browserbaserade E2E-scenarier för kritiska flöden när funktionerna stabiliserats. | Medel – planera efter implementation av 3.x-flöden |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person. Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande. | Medel – UC002-vägen får aldrig kollidera |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person simultaneously. Unikt index är sista skyddet. | Låg |
| **`Shift` saknar `EditionId`** | `Shift` har ingen direkt koppling till `EditionId`. `MyScheduleRepository` löser detta via `Edition.Stations`-navigeringen (shadow FK). Om Shift-kontexten växer bör ett direkt `EditionId` övervägas på `Shift` för att slippa join-beroendet mot Convention. | Låg – fungerar korrekt, men fragil vid schemamigration |
| **Deduplikering i tidsschema** | Om samma session förekommer i flera kategorier (t.ex. bokad OCH arrangör) prioriteras Booked > Organiser > Watching i `MyScheduleRepository`. Prioriteringslogiken är inte testad på domännivå. Om affärsreglerna ändras (t.ex. "visa alltid arrangörsrollen oavsett bokning") behöver deduplikeringen ses över. | Låg – nuvarande beteende är rimligt |
| **Inga `DbSet<Station>` i `ConventionDbContext`** | `Station` och `Venue` nås via `db.Set<T>()` i stället för namngivna `DbSet<T>`-properties. Inkonsekvens mot övriga entiteter. Lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` om fler queries börjar hämta dem direkt. | Låg |

---

## Diverse
- **Debounced sökning** – samma RxJS-pipeline (`debounceTime → distinctUntilChanged → switchMap → loading → subscribe`) i checkin och walkup. Extrahera till `createSearchStream()`-helper.
- **Multi-request pending-räknare** – `persons.component.ts` koordinerar tre parallella anrop med en manuell `pending`-räknare. Ersätt med `forkJoin`.
- reception, återkallade biljetter är sekundär info. eller skall de tas bort?
- reception, statistiken är inte tillräcklig. skall också ha antal pass och vara uppdelat per dag.
- reception, om man inte har qr-biljett (måste man kunna visa i public), så behövs något annat sätt att bekräfta biljetten.
- taggar: ska kunna sättas av arrangören redan från start som en del av grunduppgifterna.
- sidor, import/export
- taggar, import/export
