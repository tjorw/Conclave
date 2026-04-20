# Demo-deploy

Den här guiden beskriver hur den publicerade demo-artifacten för `R11` körs och verifieras som en sammanhållen instans.

## Målbild

Demo-instansen kör:

- ett API
- `public` på `/`
- `admin` på `/admin/`
- `portal` på `/portal/`
- en SQL Server-databas för både domändata och identity

Runtime-profilen är `Demo`.

## Förutsättningar

- publicerad artifact från `dotnet publish`
- en nåbar SQL Server
- miljövariabler för databas, JWT och klientlänkar
- en host som kan exponera samma origin för API och klienter

## Publish lokalt

```powershell
dotnet publish backend/src/ConventionSystem.Api -c Release -o backend/artifacts/demo-publish
```

Artifacten innehåller då:

- `wwwroot/` för `public`
- `wwwroot/admin/` för `admin`
- `wwwroot/portal/` för `portal`

## Obligatoriska miljövariabler

Sätt minst följande:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Demo"
$env:ConnectionStrings__DefaultConnection = "Server=.;Database=ConventionSystemDemo;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Jwt__Key = "replace-with-a-real-demo-secret-at-least-32-chars"
$env:Jwt__Issuer = "ConventionSystem"
$env:Jwt__Audience = "ConventionSystem"
$env:App__FrontendUrl = "https://demo.example.com"
$env:App__AdminUrlTemplate = "https://demo.example.com/admin"
$env:App__PortalUrl = "https://demo.example.com/portal"
```

Sätt dessutom e-post efter vald strategi:

- enklast för intern demo:
  `Email__Provider=Logging`
- för riktig SMTP:
  `Email__Provider=Smtp`
  `Email__Smtp__Host=...`
  `Email__Smtp__Port=...`
  `Email__Smtp__Username=...`
  `Email__Smtp__Password=...`

## Rekommenderad demo-policy

För första externa demo-deploy rekommenderas:

- `Multitenancy__Enabled=false`
- `DevData__EnableSeeding=true`
- `SystemAdminBootstrap__Enabled=false`
- `Email__Provider=Logging` om riktiga mail inte behövs

Det ger en enkel, stabil demo-instans med seedad data utan extra bootstrapflöden.

## Starta artifacten

Det enklaste lokala sättet är:

```powershell
./scripts/Run-DemoLocal.ps1
```

Det scriptet:

- sätter `ASPNETCORE_ENVIRONMENT=Demo`
- sätter lokal connection string och JWT-värden
- sätter klientlänkar för samma origin
- stänger av HTTPS-redirect för lokal körning
- startar den publicerade artifacten på `http://localhost:5099`

Om du vill ändra databas eller port:

```powershell
./scripts/Run-DemoLocal.ps1 `
  -ConnectionString "Server=.;Database=ConventionSystemDemo2;Trusted_Connection=True;TrustServerCertificate=True;" `
  -BaseUrl "http://localhost:5100"
```

Om en tidigare lokal demo-instans redan använder porten kan du låta scriptet starta om den:

```powershell
./scripts/Run-DemoLocal.ps1 -ForceRestart
```

Du kan fortfarande starta artifacten manuellt om du vill:

Windows:

```powershell
./backend/artifacts/demo-publish/ConventionSystem.Api.exe
```

Plattformsoberoende:

```powershell
dotnet ./backend/artifacts/demo-publish/ConventionSystem.Api.dll
```

Vid uppstart ska följande fungera:

- databasmigrationer körs
- `DevDataSeeder` körs om databasen är tom och `DevData__EnableSeeding=true`
- API:t börjar lyssna på den konfigurerade originen

## Lokal artifact-smoke

För snabb verifiering finns:

```powershell
./scripts/Invoke-DemoArtifactSmoke.ps1
```

Scriptet:

- startar artifacten lokalt
- verifierar `/`, `/admin/`, `/portal/`
- verifierar klientrutter
- verifierar att minst ett frontend-asset laddar
- verifierar att en backend-route inte fångas av SPA-fallback

## Post-deploy-checklista

Efter deploy ska följande verifieras manuellt:

1. Root-URL laddar `public`.
2. `/admin/` laddar admin-klienten.
3. `/portal/` laddar portal-klienten.
4. En klientrutt under `/admin/` fungerar via refresh.
5. En klientrutt under `/portal/` fungerar via refresh.
6. Databasen har migrerats utan fel.
7. Demo-data finns i instansen.
8. Admin-login fungerar med seedad demo-användare om demo-seeding används.
9. Portalens system-login fungerar om systemadmin finns provisionerad.
10. Ett API-anrop svarar som API och inte som HTML-fallback.

## Felsökning

Vanliga fel:

- `JWT-nyckel saknas i konfigurationen`
  Sätt `Jwt__Key`.
- klienterna laddar men API-anrop går fel
  Kontrollera `App__FrontendUrl`, `App__AdminUrlTemplate`, `App__PortalUrl` och eventuell reverse proxy.
- seedning sker inte
  Kontrollera `ASPNETCORE_ENVIRONMENT=Demo` och `DevData__EnableSeeding=true`.
- SPA-rutter fungerar bara första gången
  Kontrollera att API:t kör med publicerad `wwwroot` och att reverse proxy inte bryter path-prefixen.
