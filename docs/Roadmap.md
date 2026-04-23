# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – återstående arbete, högst prioritet överst.

- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent
- [ ] `R-HL01` Hjälpsystem – `HelpTooltip`-komponent och initiala texter för Convention/Edition (UC-HL001)
- [ ] `R-HL02` Hjälpsystem – `HelpDrawer` + `HelpService` med route-mappning (UC-HL003, UC-HL004)
- [ ] `R-HL03` Hjälpsystem – första omgången Markdown-innehåll (6 filer: convention, event, registration, staff)
- [ ] `R-HL04` Hjälpsystem – `HelpPanel`-komponent på listsidor (UC-HL002)
- [ ] `R-HL05` Hjälpsystem – tooltip-täckning för Event, Registration, Staff
- [ ] `R-OB01` Outbox-mönster för extern kommunikation – `OutboxMessage`-tabell, `OutboxEmailSender` (implementerar `IEmailSender`), `OutboxProcessor` (`IHostedService`, kör var 30:e sekund), Polly-retry med exponentiell backoff. Se `docs/Outbox.md` för design.
- [x] `R30` schemaönskemål förenklas till ett enda fritextfält – arrangören skriver sina önskemål fritt; fältet visas i schemaläggningsvyn så att schemaläggaren ser det i sitt arbetsflöde. Strukturerade fält (tidspreferenser, konflikter m.m.) tas bort.

### Bemanningsschemaläggning (R-ST-SCH)

Målet är en motsvarande schemaläggningsupplevelse för bemanning som den som finns för programpass: koordinatorn ska kunna se konventets bemanningsbehov över tid, upptäcka underbemanning och skapa eller justera pass utan att hoppa mellan stationstabeller.

**Resonemang:**
- Domänen har redan kärnan: `Shift`, `StaffAssignment`, min/max-bemanning, ansvarig person och livscykel för tilldelningar. Det behövs därför inte ett nytt planeringsaggregat.
- Nuvarande adminvy är station-för-station med tabeller och inline-detaljer. Den fungerar för administration men ger svag överblick över tid, parallella stationer och bemanningsluckor.
- Sessionsschemaläggningen har redan ett etablerat UI-mönster med tidslinje, daggränser från `Edition.ScheduleDays`, konfliktindikering och tabellalternativ. Bemanning bör återanvända samma tänk så att koordinatorer känner igen arbetsflödet.
- Bemanning skiljer sig från programpass genom att överlapp för person är en varning, inte ett stopp. Vyn ska därför visa risker och luckor tydligt utan att ändra domänregeln att koordinatorn har sista ordet.
- Backend listar i dag pass per station. En hel bemanningsvy skulle annars kräva många frontend-anrop. En edition- eller staff-area-scopad query-projektion bör införas först.

**Lösningsförslag:**
- Lägg till en query-projektion för bemanningsschema, t.ex. `GET /editions/{editionId}/staff-schedule` eller `GET /staff-areas/{staffAreaId}/schedule`.
- Returnera rader som `StaffArea -> Station` och pass som block med `shiftId`, station, ansvarig, start/slut, min/max, antal aktiva tilldelningar, antal bekräftade tilldelningar och status.
- Bygg en `StaffTimelineComponent` i admin som återanvänder principerna från `SessionTimelineComponent`: daghuvud, timmarkörer, horisontell scroll och block per station.
- Färgkoda block efter bemanningsläge: obemannad, under min, inom krav, full, över max om sådan data någonsin kan uppstå, samt inställd.
- Behåll tabellvyn som kompletterande arbetsyta för sortering, tilldelningsstatus och exakta detaljer.
- Visa tillgänglig personal i sidopanel eller detaljpanel med indikatorer för stationsönskemål, registrerad tillgänglighet och överlappande pass. Detta är beslutsstöd, inte automatisk optimering.
- Lägg till redigering av pass först när översiktsvyn finns: ändra tid, station, ansvarig och bemanningskrav via ett nytt `UpdateShiftCommand`.

**Utvecklingssteg:**
- [ ] `R-ST-SCH01` Backend-query för bemanningsschema per upplaga eller funktionsområde. Inkludera stationer, pass, bemanningsgrad, ansvarig och status i en samlad DTO.
- [ ] `R-ST-SCH02` Frontend-service och modeller för staff-schedule-projektionen i `shared`.
- [ ] `R-ST-SCH03` `StaffTimelineComponent` med rader per station, tidslinje per `Edition.ScheduleDays`, block för pass och visuella lägen för underbemanning/fullbemanning/inställt.
- [ ] `R-ST-SCH04` Integrera tidslinjen i adminens bemanningsvy med växling mellan tidslinje och tabell samt filter på dag, funktionsområde, station och bemanningsstatus.
- [ ] `R-ST-SCH05` Skapa pass direkt från schemavyn med förifylld station och tid. Återanvänd befintligt `CreateShiftCommand`.
- [ ] `R-ST-SCH06` Detaljpanel för pass: tilldelningar, bekräfta/neka/avboka, och tilldela person med varningar för otillgänglighet och överlapp.
- [ ] `R-ST-SCH07` `UpdateShiftCommand` och endpoint för att ändra tid, station, ansvarig och bemanningskrav på planerade pass.
- [ ] `R-ST-SCH08` Tester: application-querytester för projektionen, commandtester för uppdatering av pass och fokuserade frontendtester för filter/visuella bemanningslägen.
- [ ] `R-ST-SCH09` Dokumentera och städa regelkonflikten mellan use case-texten som säger att vilken person som helst kan tilldelas och implementationen/README som kräver godkänd staffansökan.

### Laganmälningar (R-TM)

- [ ] `R-TM01` `Event.RegistrationMode: Individual | Team` – konfiguration per evenemang (nytt fält på Event-aggregatet)
- [ ] `R-TM02` `Team`-aggregat – Edition-scoped, captain (`PersonId`), `Members[]` (namn + valfritt `PersonId`)
- [ ] `R-TM03` `TeamEventRegistration`-aggregat – lag anmäler sig till evenemang, livscykel `Pending → Confirmed | Cancelled`
- [ ] `R-TM04` Admin-vy: arrangör tilldelar lag till session (`TeamSessionAssignment` på `Session`)
- [ ] `R-TM05` Tidschema: lagmedlemmars tilldelade sessioner visas via query-projektion (utökning av `MyScheduleRepository`)
- [ ] `R-TM06` Publik vy: laganmälningsflöde – captain anmäler lag, anger medlemmar; en person per bokning

### Multitenancy

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [ ] `R-MT014` `portal`-app: self-service signup (publik del)
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

### Rikt innehåll (R-RC)

- [ ] `R-RC01` Markdown-editor för eventbeskrivningar – admin kan redigera publik beskrivning med formatering; arrangör kan redigera sin del
- [ ] `R-RC02` Bilduppladdning – endpoint + lagring (lokal disk eller blob); bilder refereras via URL i Markdown-innehåll
- [ ] `R-RC03` Redaktionella informationssidor – `Page`-entitet med `slug`, `title`, `content`; admin skapar/redigerar, publika appen renderar
- [ ] `R-RC04` Mailmallar – Markdown-template med variabler (`{{firstName}}` m.fl.), renderas server-side till HTML vid utskick

### Programtaggar (R-TAG)

- [ ] `R-TAG01` Taggdefinitioner på `Edition` – `Edition` äger en uppsättning taggdefinitioner som value objects, t.ex. `Barnvänligt`, `18+`, `Nybörjare`. Taggar är upplagespecifika och ska kunna administreras tillsammans med övrig edition-struktur.
- [ ] `R-TAG02` Tillämpa taggar på `Event` – evenemang refererar endast till taggar som finns definierade på samma `Edition`. Validering ska hindra okända taggar och taggar från annan upplaga.
- [ ] `R-TAG03` Publik exponering och filtrering – event-feed och programdetalj visar taggar; publika programvyn erbjuder taggfilter utöver dag och kategori.
- [ ] `R-TAG04` Kopiering av struktur – `CopyStructure` bör kopiera editionens taggdefinitioner på samma sätt som lokaler, funktionsområden och stationer när det är relevant för ny upplaga.

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
| **`ICurrentUser` i bakgrundsjobb (dokumentera)** | `ICurrentUser` läser från `HttpContext` – fungerar inte utanför HTTP-request-scopet. Bakgrundsjobb och seeders måste anropa domänmodellen direkt eller använda systemidentitet. Outbox-processorn är referensimplementation för rätt mönster (`R-OB01`). | Medel |
---

## Fas 4 – Demo och driftsättning

### 4.1 Demo-deploy (ett fiktivt konvent)
- Bygg-pipeline: Angular-appar (admin + publik +  portal) byggs in i `wwwroot` som en del av .NET publish-steget
- En SQL Server-instans med en databas (`dbo` för domändata, `identity` för ASP.NET Identity)
- Self-contained .NET-publish deployad till en host (VPS, Azure App Service eller liknande)
- `DevDataSeeder` körs i `Development`-miljö och skapar demo-konvention med exempeldata
- Hemligheter via miljövariabler eller Key Vault (ej `appsettings`)
- Första verifieringsmålet för `R11` är en lokal publishbar demo-artifact som kan startas utan frontend-devserver och servera de paketerade klienterna från publish-outputen

### 4.2 Konvent-onboarding
Varje konvention är en separat deploy. Onboarding innebär att sätta upp en ny instans:
- Ny databas provisioneras (kör EF Core-migrationer mot `DefaultConnection`)
- `environment.ts` konfigureras med rätt `conventionId` och `apiBaseUrl`
- Admin-konto skapas via `CreateConventionCommand` + `UserManager`
- Välkomstmejl med inloggningsuppgifter för konventets admin


## Refine
- Bakgrundsjobb för mail m.m. → se `R-OB01` (Outbox-mönster, design i `docs/Outbox.md`)
- Föreslå startdatum i datum kontroller som är första dagen på konventet
- public - funktionering skall visa funktionärsbiljetter
- arrangör - skall visa arrangörsbiljetter
- biljetter till arranggörer - man behöver bli tilldelad
- bokningar väntlista - man hamnar där först
- bokningar i arrangmang (tilldelning)
-- först går först
-- lottning
-- manuell
- språkstyrning
-- engelsk version
- default start och sluttid på evenemanget, men sätt per dag
