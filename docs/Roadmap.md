# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – återstående arbete, högst prioritet överst.

- [x] `R-SCH03` Datumkontroller i boknings-, pass- och sessionsflöden föreslår första konventsdagen och dagens standardtider där det passar användarflödet.

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

Kvar att göra:
- [x] `R-RC04` Mailmallar – adminredigerbara mallar i databas; standardmall per typ i kod (restore-funktion); `TemplateRenderer` med Markdig + variabelsubstitution

**Implementationsplan (UC-RC005, UC-RC006) – se ADR `docs/decisions/2026-05-08-mail-templates.md`:**
1. **Domän** – `MailTemplate` (aggregatrot), `MailTemplateType` (enum, 7 typer), `MailTemplateId`
2. **Applikation** – `IMailTemplateRenderer`, `DefaultMailTemplates`, `IMailTemplateRepository`, commands (`UpdateMailTemplate`, `ResetMailTemplate`), queries (`GetMailTemplate`, `ListMailTemplates`)
3. **Infrastruktur** – `MarkdigMailTemplateRenderer`, `MailTemplateRepository`, EF Core-konfiguration (`mail_templates`-tabell), migration, uppdaterade `IEmailService`-signaturer med `ConventionId`, `OutboxEmailService` integrerar renderer
4. **API** – `MailTemplateEndpoints` (5 endpoints under `/api/conventions/{id}/mail-templates`)
5. **Frontend** – `mail-templates`-feature i admin: lista + markdown-redigeringsvy med variabelhjälp och "Återställ"-knapp


### Varumärke per konvent (R-BR)

Publika appen ska reflektera respektive konvents grafiska profil utan redeploy. Se UC-BR001–UC-BR002 i `docs/UseCases.md`.

Implementationsordning: R-BR01 → R-BR02

- [x] `R-BR01` `ConventionBranding`-entitet (ConventionId, PrimaryColor, AccentColor, LogoUrl, FaviconUrl, FontFamily, CustomCss) – upsert-semantik; endpoint `PUT /api/conventions/{id}/branding`; anonym `GET`-endpoint med `Cache-Control: max-age=300`; admin-UI med färgväljare, filuppladdning och typsnittsval (UC-BR001)
- [x] `R-BR02` Publik shell hämtar branding vid initialisering och applicerar CSS-variabler via `document.documentElement.style.setProperty`; logotyp sätts i navbar; fallback till systemdefinierade värden om anropet misslyckas (UC-BR002)

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

## Övrig backlog
- **Debounced sökning** – extrahera gemensam RxJS-pipeline till `createSearchStream()`-helper.
- **Multi-request pending-räknare** – ersätt manuell `pending`-räknare i `persons.component.ts` med `forkJoin`.
- **Reception: UI och statistik** – avgör om återkallade biljetter ska visas, utöka statistik med antal pass och uppdelning per dag.
- **Reception: alternativ verifiering** – definiera flöde för verifiering när besökaren saknar QR-biljett.
- **Taggar i grundflöde** – låt arrangör sätta taggar redan i grunduppgifter.
- **Import/export** – lägg till stöd för sidor och taggar.
