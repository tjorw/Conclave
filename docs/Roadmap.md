# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – återstående arbete, högst prioritet överst.

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
| **`/system`-bypass i `TenantResolutionMiddleware`** | Middleware bypassar hela `/system`-prefixet för systemadmin- och signup-flöden. Detta är avsiktligt, men bör dokumenteras/testas som ett kontrakt så att nya `/system/*`-endpoints inte råkar förväntas ha tenant-context. Behåll integrationstester för `/system/auth/login`, `/system/signup` och skyddade `/system/tenants/*`. | Medel |
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

#### `AP1` Beslut: hostingmodell för demo-deploy
`AP1` landas som **en origin, ett API, path-baserad hosting för admin och portal**.

Föreslagen URL-struktur för demo:
- `https://demo-host/` → `public`
- `https://demo-host/admin/` → `admin`
- `https://demo-host/portal/` → `portal`
- `https://demo-host/...` → API-endpoints på samma origin, utan separat frontend-devserver

Motivering:
- Detta stödjer roadmapens mål om en lokal publishbar artifact som kan startas och testas som en sammanhållen instans.
- Det minskar driftkomplexiteten jämfört med separata origins eller flera hostar för samma demo.
- Det gör det möjligt att verifiera hela demo-deployn lokalt utan extra proxy, separat webbserver eller flera processer för klienterna.
- `public` bör ligga på roten `/` eftersom den fungerar som den naturliga startsidan för demo-instansen.
- `admin` och `portal` passar bättre under egna path-prefix eftersom de annars konkurrerar med `public` om root-routes som `/login`.

Konsekvenser för kommande arbetspaket:
- `admin` måste byggas med `baseHref` och asset-path för `/admin/`.
- `portal` måste byggas med `baseHref` och asset-path för `/portal/`.
- `public` kan fortsatt använda `/` som base href.
- Backend behöver servera tre separata SPA-entrypoints och bara falla tillbaka till respektive `index.html` för klientrutter, aldrig för API-rutter.
- Frontendkonfigurationen bör på sikt använda samma origin som default för `apiBaseUrl` i demo-artifacten, i stället för hårdkodade externa API-adresser.

Alternativ som väljs bort i `AP1`:
- **Separata origins per app** väljs bort för demo-spåret eftersom det ökar CORS-behov, lokal testkomplexitet och deployberoenden innan `R11` ens kan verifieras.
- **Path-baserad hosting för alla tre appar inklusive `public` under `/public/`** väljs bort eftersom det gör demo-instansen mindre naturlig att navigera till och ger sämre startsideupplevelse.

#### `AP2` Status: frontend-build för deploy
Detta är nu delvis genomfört och verifierat lokalt.

Genomfört:
- `admin` byggs för `/admin/`
- `public` byggs för `/`
- `portal` byggs för `/portal/`
- varje app har separat output-path under `frontend/dist/`
- `portal` har nu en separat `environment.prod.ts`
- produktionsmiljöerna för klienterna använder relativ `apiBaseUrl` för demo-artifacten i stället för placeholder-URL
- gemensamma npm-scripts finns för `build:admin:prod`, `build:public:prod`, `build:portal:prod` och `build:demo`

Verifierat:
- `ng build admin --configuration production`
- `ng build public --configuration production`
- `ng build portal --configuration production`

Kvar inom `AP2`:
- vid behov dokumentera hur dessa build-outputar ska kopieras in under `wwwroot` i nästa steg (`AP3`)
- ta ställning till om fler deployspecifika environment-värden behöver tillföras innan publish-integrationen byggs

#### `AP3` Status: inbäddning i API-publish
Detta är nu genomfört och verifierat lokalt.

Genomfört:
- `ConventionSystem.Api.csproj` kör frontend-build under `dotnet publish`
- publish-flödet kör `npm ci` och därefter `npm run build:demo`
- `public` kopieras till `wwwroot/`
- `admin` kopieras till `wwwroot/admin/`
- `portal` kopieras till `wwwroot/portal/`
- den lokala demo-artifacten kan nu produceras med ett enda `dotnet publish`

Verifierat:
- `dotnet publish backend/src/ConventionSystem.Api/ConventionSystem.Api.csproj -c Release -o backend/artifacts/demo-publish`
- publish-output innehåller:
  `backend/artifacts/demo-publish/wwwroot/index.html`
  `backend/artifacts/demo-publish/wwwroot/admin/index.html`
  `backend/artifacts/demo-publish/wwwroot/portal/index.html`
- respektive `index.html` har korrekt `base href` för root, `/admin/` och `/portal/`

Kvar inom `AP3`:
- inget större implementationsarbete återstår i själva paketeringen
- eventuellt kan `npm ci` senare optimeras bort eller göras villkorligt i vissa CI-scenarier, men det blockerar inte `R11`

Nästa steg:
- `AP4` behöver lägga till statisk filservering och SPA-fallbacks i API:t så att den publicerade artifacten också går att köra som en sammanhållen instans

#### `AP4` Status: statiska filer och SPA-routing i API
Detta är nu implementerat i API:t och verifierat på build- och publish-nivå.

Genomfört:
- `Program.cs` använder `UseDefaultFiles` och `UseStaticFiles` när `wwwroot` finns
- root-artifacten kan servera `public` från `/`
- fallback för `admin` finns på `/admin/{*path:nonfile}`
- fallback för `portal` finns på `/portal/{*path:nonfile}`
- SPA-hostingen aktiveras bara när `wwwroot` existerar, så lokal utveckling med `dotnet run` utan publicerad frontend påverkas inte

Verifierat:
- `dotnet build backend/src/ConventionSystem.Api/ConventionSystem.Api.csproj -c Release`
- `dotnet publish backend/src/ConventionSystem.Api/ConventionSystem.Api.csproj -c Release -o backend/artifacts/demo-publish`

Kvar inom `AP4`:
- köra en riktig runtime-smoke mot den publicerade artifacten med fungerande databas och runtime-konfiguration
- bekräfta manuellt att klientrutter under `/admin/...` och `/portal/...` laddar korrekt i browser

Statusuppdatering:
- en automatiserad lokal artifact-smoke finns nu som `scripts/Invoke-DemoArtifactSmoke.ps1`
- den verifierar att publishad artifact startar, att SPA-entrypoints svarar korrekt och att API-routes inte fångas av fallback-routing

#### `AP5` Status: demo-konfiguration och secrets
Detta är nu delvis implementerat och dokumenterat.

Genomfört:
- separat runtime-profil finns i `appsettings.Demo.json`
- demo-profilen utgår från en origin och samma path-baserade klientstruktur som `AP1`
- `DevData:EnableSeeding` styr nu seeding explicit i stället för att vara implicit kopplat till `Development`
- `DevDataSeeder` får bara köras i `Development` eller `Demo`
- README dokumenterar vilka miljövariabler som krävs för en riktig demo-instans

Definierade runtime-värden för demo:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `App__FrontendUrl`
- `App__AdminUrlTemplate`
- `App__PortalUrl`
- e-postinställningar via `Email__*`

Kvar inom `AP5`:
- bestäm om demo ska använda `Email:Logging` eller riktig SMTP/leverantör i den första externa deployen
- ta ställning till om `SystemAdminBootstrap` ska användas i demo eller om allt ska seedas via separat provisioning
- när `AP6` byggs klart bör CI kunna injecta dessa värden utan lokala filer

#### `AP6` Status: publish-artifact i CI
Detta är nu implementerat i GitHub Actions.

Genomfört:
- separat jobb `demo_artifact` finns i `ci.yml`
- jobbet kör efter ordinarie build och test
- jobbet kör `dotnet publish` för demo-instansen
- publicerad artifact laddas upp som `demo-publish-linux`
- jobbet kör den lokala artifact-smoken som en del av verifieringen
- demo-konfiguration injiceras i CI via miljövariabler i workflowen i stället för lokala settings-filer

CI-verifiering i `AP6` omfattar:
- publicering av API + inbyggda klienter
- uppstart mot SQL Server-service i workflowen
- smoke-test av `/`, `/admin/`, `/portal/` och klientrutter
- kontroll att API-routes inte fångas av SPA-fallback

Kvar inom `AP6`:
- eventuellt publicera även zip-paket eller plattformsspecifika artifacts senare
- vid behov lägga till manuell deploy eller release-workflow ovanpå artifact-jobbet

#### `AP7` Status: driftguide och smoke-test
Detta är nu dokumenterat.

Genomfört:
- separat driftguide finns i `docs/DemoDeploy.md`
- README länkar till demo-guiden
- guiden beskriver:
  obligatoriska miljövariabler
  rekommenderad demo-policy
  start av publicerad artifact
  lokal artifact-smoke
  manuell post-deploy-checklista
  felsökning

Post-deploy-smoken för `R11` omfattar nu uttryckligen:
- `public` laddar på `/`
- `admin` laddar på `/admin/`
- `portal` laddar på `/portal/`
- klientrutter fungerar via refresh under `admin` och `portal`
- databasen migrerar
- demo-data finns
- API-routes beter sig som API och inte som SPA-fallback

Kvar inom `AP7`:
- vid första riktiga externa deploy bör checklistan köras skarpt och därefter justeras utifrån verkliga driftlärdomar

### 4.2 Konvent-onboarding
Varje konvention är en separat deploy. Onboarding innebär att sätta upp en ny instans:
- Ny databas provisioneras (kör EF Core-migrationer mot `DefaultConnection`)
- `environment.ts` konfigureras med rätt `conventionId` och `apiBaseUrl`
- Admin-konto skapas via `CreateConventionCommand` + `UserManager`
- Välkomstmejl med inloggningsuppgifter för konventets admin
