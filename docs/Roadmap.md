# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – återstående arbete, högst prioritet överst.

- [x] `R24` Bygg publik vy för inlösen av promotionkod (UC-PC003)
- [x] `R25` Förbättra sessions-UX i klienter: global auth-status, sessionvarning före utgång och tydlig 401/403/nätverksbanner
- [ ] `R22` Centralisera JWT-konfigurationsnycklar (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`)
- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent
- [ ] `R-HL01` Hjälpsystem – `HelpTooltip`-komponent och initiala texter för Convention/Edition (UC-HL001)
- [ ] `R-HL02` Hjälpsystem – `HelpDrawer` + `HelpService` med route-mappning (UC-HL003, UC-HL004)
- [ ] `R-HL03` Hjälpsystem – första omgången Markdown-innehåll (6 filer: convention, event, registration, staff)
- [ ] `R-HL04` Hjälpsystem – `HelpPanel`-komponent på listsidor (UC-HL002)
- [ ] `R-HL05` Hjälpsystem – tooltip-täckning för Event, Registration, Staff

### Multitenancy

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [x] `R-MT013` `portal`-app: provisioneringsvy för systemadmin
- [ ] `R-MT014` `portal`-app: self-service signup (publik del)
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare
- [x] `R-MT016` Välkomstmail vid provisioning
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| **`/system/auth`-bypass i `TenantResolutionMiddleware`** | Rad 18 i middleware bypasas för sökvägar som börjar med `/system/auth`, men ingen sådan endpoint finns. Antingen bör bypasset tas bort, eller skapas endpointen och ett test som verifierar bypasset. Tyst bypass mot icke-existerande endpoint är ett underhållsproblem. | Hög |
| **`TenantLookupDbContext` bör använda `NoTracking`** | Resolvern läser enbart – change tracking är onödigt overhead. Lägg till `.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` i factory-registreringen i `InfrastructureServiceExtensions`. | Medel |
| **Tenant-resolver TTL bör höjas** | `CachingTenantResolver` har TTL på 60 s. Nu när `ITenantResolverCacheInvalidator` kallas vid suspend/restore motiverar det ett längre TTL (~5 min) för att minska DB-belastning. | Medel |
| **Ingen loggning i `TenantResolutionMiddleware`** | `tenant_not_found` och `tenant_suspended` returnerar felkod men loggar ingenting. `ILogger`-injektion med `Warning`-loggning förenklar felsökning i produktion. | Medel |
| **Cache stampede i `CachingTenantResolver`** | Mönstret `TryGetValue → miss → DB → Set` utan lås ger N parallella DB-träffar vid burst mot okänd tenant. `GetOrCreateAsync` eller en `SemaphoreSlim` per nyckel eliminerar problemet. Låg risk vid nuvarande skala. | Låg |
| **Oanvänd `using` i `InfrastructureServiceExtensions`** | `using Microsoft.Extensions.Caching.Memory` används inte i filen (`AddMemoryCache()` är en extension method i `Microsoft.Extensions.DependencyInjection`). Bör tas bort. | Låg |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| **Feed-cachning och API-nyckel** | Feed-endpointsen är öppna och läser från databasen vid varje anrop. Vid hög trafik bör svaren cachas (HTTP-headers `Cache-Control`/`ETag`, CDN-lager eller Redis). Vid behov av skyddade feeds kan en API-nyckel läggas till utan att ändra URL-strukturen. | Medel – utvärdera inför produktion |
| **E2E-test för journeys** | Journey-flöden saknar UI-verifiering över hela kedjan. Lägg till browserbaserade E2E-scenarier för kritiska flöden när funktionerna stabiliserats. | Medel – planera efter implementation av 3.x-flöden |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person. Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande. | Medel – UC002-vägen får aldrig kollidera |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person simultaneously. Unikt index är sista skyddet. | Låg |
| `ICurrentUser` i bakgrundsjobb | `ICurrentUser` läser från `HttpContext` och fungerar inte utanför HTTP-request-scopet. Bakgrundsjobb och seeders måste anropa domänmodellen direkt. | Medel – dokumentera mönstret |
| **`Shift` saknar `EditionId`** | `Shift` har ingen direkt koppling till `EditionId`. `MyScheduleRepository` löser detta via `Edition.Stations`-navigeringen (shadow FK). Om Shift-kontexten växer bör ett direkt `EditionId` övervägas på `Shift` för att slippa join-beroendet mot Convention. | Låg – fungerar korrekt, men fragil vid schemamigration |
| **Deduplikering i tidsschema** | Om samma session förekommer i flera kategorier (t.ex. bokad OCH arrangör) prioriteras Booked > Organiser > Watching i `MyScheduleRepository`. Prioriteringslogiken är inte testad på domännivå. Om affärsreglerna ändras (t.ex. "visa alltid arrangörsrollen oavsett bokning") behöver deduplikeringen ses över. | Låg – nuvarande beteende är rimligt |
| **Inga `DbSet<Station>` i `ConventionDbContext`** | `Station` och `Venue` nås via `db.Set<T>()` i stället för namngivna `DbSet<T>`-properties. Inkonsekvens mot övriga entiteter. Lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` om fler queries börjar hämta dem direkt. | Låg |
| **R22: Centralisera JWT-konfigurationsnycklar** | Nycklarna `Jwt:Key`, `Jwt:Issuer` och `Jwt:Audience` används duplicerat i startup och auth. Samla i konstanter/options för att minska typo-risk och förenkla ändringar. | Medel |
| Outbox | Om en extern tjänst inte är tillgänglig så går applikationen sönder. T.ex. om SMPT inte är tillgängligt. Dessa aktiviteter behöver hanteras persistent i en outbox och ha en backgroundworker. Viktigt att den inte har koppling till http-kontextet | Hög |
| dbcontentexts | skall inte finnas i api-lagret | Hög |
---

## Fas 4 – Demo och driftsättning

### 4.1 Demo-deploy (ett fiktivt konvent)
- Bygg-pipeline: Angular-appar (admin + publik +  portal) byggs in i `wwwroot` som en del av .NET publish-steget
- En SQL Server-instans med en databas (`dbo` för domändata, `identity` för ASP.NET Identity)
- Self-contained .NET-publish deployad till en host (VPS, Azure App Service eller liknande)
- `DevDataSeeder` körs i `Development`-miljö och skapar demo-konvention med exempeldata
- Hemligheter via miljövariabler eller Key Vault (ej `appsettings`)
- Första verifieringsmålet för `R11` är en lokal publishbar demo-artifact som kan startas utan frontend-devserver och servera de paketerade klienterna från publish-outputen

#### Förutsättningar att lösa för `R11`
- API:t serverar i nuläget inte statiska frontendfiler. `Program.cs` saknar `UseDefaultFiles`, `UseStaticFiles` och fallback-routing för SPA.
- API-projektets publish-flöde bygger inte frontend. `ConventionSystem.Api.csproj` saknar targets eller script som kör Angular-build och kopierar output till publish-artifacten.
- CI producerar ingen deploybar artifact idag. Workflowen kör restore, build och test men ingen `.NET publish` och ingen uppladdning av färdigt paket.
- `admin` och `public` har produktionsfiler som fortfarande pekar på placeholders för `apiBaseUrl` och `conventionId`. De är inte deployklara som demo-konfiguration.
- `portal` saknar separat `environment.prod.ts`, vilket gör appen oförberedd för samma releaseflöde som de andra klienterna.
- Angular-apparna saknar dokumenterad strategi för gemensam hosting. Det är inte bestämt om demo ska köras med path-baserade appar (`/admin`, `/public`, `/portal`) eller separata origins.
- Om apparna ska hostas under samma API måste `baseHref` och output-struktur per app definieras så att deras `index.html` och assets inte krockar i `wwwroot`.
- Demo-deployn behöver en tydlig runtime-profil. Det måste beslutas om den ska köras som `Development` eller en särskild `Demo`-miljö, eftersom det påverkar seedning och annan miljöstyrd logik.
- Hemligheter och driftvärden behöver lyftas ur repo-bundna settings. `DefaultConnection`, `Jwt:*`, e-postinställningar och host-specifika länkar måste kunna sättas via miljövariabler eller motsvarande.
- Det behöver finnas en verifierad smoke-testlista för den färdiga artifacten: databas migrerar, demo-data seedas, `public` laddar, `admin` login fungerar och `portal` laddar.

#### Arbetspaket för förutsättningarna
- `AP1` Hostingmodell för klienterna
  Bestäm URL-strategi för demo-deployn: path-baserad hosting i samma API eller separata origins. Dokumentera beslutet eftersom det styr Angular build, backend routing och deploymiljö.
- `AP2` Frontend-build för deploy
  Lägg till produktionskonfiguration för alla appar, särskilt `portal`, och definiera output-struktur samt eventuell `baseHref`/asset-path per app.
- `AP3` Inbäddning i API-publish
  Utöka `ConventionSystem.Api.csproj` eller kompletterande buildscript så att frontend byggs och kopieras in i publish-outputen under `wwwroot`.
- `AP4` Statiska filer och SPA-routing i API
  Lägg till middleware och fallback-routes i `Program.cs` så att de inbyggda klienterna faktiskt kan serveras från den publicerade API-instansen.
- `AP5` Demo-konfiguration och secrets
  Definiera vilka miljövariabler som krävs för demo, hur de mappar till API och frontend, samt hur `DevDataSeeder` ska styras på ett säkert och tydligt sätt.
- `AP6` Publish-artifact i CI
  Lägg till ett separat jobb i GitHub Actions som kör `dotnet publish`, paketerar demo-instansen och publicerar artifacten för nedladdning eller vidare deploy.
- `AP7` Driftguide och smoke-test
  Skriv en kort körguide för demo-instansen och en checklista för verifiering efter deploy så att `R11` kan bedömas som faktiskt klart och inte bara byggbart.
  Den första smoke-testen ska kunna köras lokalt mot den publicerade artifacten, inte via `dotnet run` + `ng serve`.

### 4.2 Konvent-onboarding
Varje konvention är en separat deploy. Onboarding innebär att sätta upp en ny instans:
- Ny databas provisioneras (kör EF Core-migrationer mot `DefaultConnection`)
- `environment.ts` konfigureras med rätt `conventionId` och `apiBaseUrl`
- Admin-konto skapas via `CreateConventionCommand` + `UserManager`
- Välkomstmejl med inloggningsuppgifter för konventets admin
