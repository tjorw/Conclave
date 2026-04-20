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
- [ ] `R-MT016` Välkomstmail vid provisioning
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

### 4.2 Konvent-onboarding
Varje konvention är en separat deploy. Onboarding innebär att sätta upp en ny instans:
- Ny databas provisioneras (kör EF Core-migrationer mot `DefaultConnection`)
- `environment.ts` konfigureras med rätt `conventionId` och `apiBaseUrl`
- Admin-konto skapas via `CreateConventionCommand` + `UserManager`
- Välkomstmejl med inloggningsuppgifter för konventets admin
