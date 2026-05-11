# ADR: Slutföra multitenancy – lokal dev-workflow, demo SaaS-miljö och Fas 4

**Datum:** 2026-05-11  
**Status:** Föreslagen

---

## Kontext

Fas 1–3 av multitenancy är färdigimplementerade:

- **Fas 1** (R-MT001–R-MT004): `Tenancy` bounded context, EF Core row-level isolation, `TenantSeedInterceptor`, `TenantResolutionMiddleware`, SystemAdmin-roll och policy.
- **Fas 2** (R-MT005–R-MT009): `ApplicationUser` med `UserType`/`TenantId`, filtrerade identity-index, `TenantAwareUserService`, tenant CRUD med handlers och endpoints, tenant-resolving med 60 s TTL-cache, autentisering med separata login-ingångar, provisionerings-endpoint.
- **Fas 3** (R-MT010–R-MT012): `tenantDevInterceptor` i shared-biblioteket, portal-app med grundstruktur och tenant-hantering (lista, skapa, suspendera, återaktivera, provisionera).

Tre saker saknas för att multitenancy ska vara fullt produktionsredo:

1. **Lokalt dev-workflow** – Ingen etablerad väg för att köra i SaaS-läge lokalt. `appsettings.Development.json` har `Multitenancy:Enabled=false`. Dev-seedern skapar bara en tenant. Angular-apparna har inga förkonfigurerade `devTenantId`. Det finns inget script för att starta backend i SaaS-läge.

2. **Demo SaaS-miljö** – Befintlig `appsettings.Demo.json` kör med `Multitenancy:Enabled=false`. Det finns inget sätt att demonstrera tenant-isolation eller multi-tenant-flödet med pre-seedade exempeltenants.

3. **Fas 4 (delvis)** – R-MT013 (provisioneringsvy i portal-appen) är klar. R-MT014 (self-service signup) och R-MT015 (tenant-dashboard för tenant-ägare) saknas.

---

## Beslut

### A. Lokal SaaS-dev-workflow (R-MT018)

#### Backend

Ny fil `appsettings.SaaS.json` aktiverar multitenancy och seeding i SaaS-läge:

```json
{
  "Multitenancy": {
    "Enabled": true
  },
  "DevData": {
    "EnableSeeding": true,
    "SaaSMode": true
  },
  "SystemAdminBootstrap": {
    "Enabled": true,
    "Email": "systemadmin@local.dev",
    "Password": "Admin123!"
  }
}
```

Dev-seedern utökas med en SaaS-gren som aktiveras av `DevData:SaaSMode=true`. Den skapar **deterministiska tenant-IDs** (hardkodade UUIDs) och körs idempotent:

| Tenant | Subdomän | TenantId (deterministisk) | Konvention | Upplaga |
|--------|----------|--------------------------|------------|---------|
| Gammacon | `gammacon` | `a0000001-0000-7000-8000-000000000001` | "Gammacon" | Full (venues, areas, events) |
| Länsen | `lansen` | `a0000002-0000-7000-8000-000000000002` | "Länsen" | Minimal (bara skapad) |

Deterministiska IDs är viktiga – Angular-apparna kan pre-konfigurera `devTenantId` utan att utvecklaren behöver slå upp ID:t manuellt efter varje seed.

Gammacon-seedningen återanvänder nuvarande single-tenant seed (venues, areas, stations, kategorier, events). Länsen-seedningen skapar bara konventionen – testar tomma tenants i portal-appen.

Administratörer per tenant skapas med:

| Tenant | Email | Lösenord |
|--------|-------|----------|
| Gammacon | `admin@gammacon.local` | `Admin123!` |
| Länsen | `admin@lansen.local` | `Admin123!` |

#### Ny script: `scripts/Run-SaaSLocal.ps1`

Skriptet kör `dotnet run` (inte publicerad artifact) med SaaS-profil. Det är anpassat för daglig utveckling – inte demo.

```powershell
# Starta backend i SaaS-läge lokalt
./scripts/Run-SaaSLocal.ps1
```

Parametrar (med standardvärden):
- `-Port 5127` – API-port
- `-ConnectionString "Server=.;Database=ConventionSystemSaaS;..."` – separat dev-databas
- `-JwtKey "..."` – JWT-nyckel

Skriptet sätter `ASPNETCORE_ENVIRONMENT=SaaS` och startar via `dotnet run`. Ingen publicering krävs.

#### Frontend: `devTenantId` i environment-filer

Angular-apparna `admin` och `public` konfigureras med Gammacon-tenantens deterministiska ID i development-environment:

```typescript
// frontend/projects/admin/src/environments/environment.development.ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5127',
  multitenancy: {
    enabled: true,
    devTenantId: 'a0000001-0000-7000-8000-000000000001'  // Gammacon
  }
};
```

För att testa Länsen: ändra `devTenantId` till Länsen-ID i environment-filen och starta om dev-servern. `tenantDevInterceptor` (redan implementerad) läser `devTenantId` och sätter `X-Tenant-ID`-headern automatiskt.

#### README-tillägg

Nytt avsnitt "SaaS-läge lokalt" under "Daglig utveckling":

```
# SaaS-läge (multitenancy aktiverat)
./scripts/Run-SaaSLocal.ps1

# Angular: devTenantId i environment.development.ts styr vilken tenant som testas
# Gammacon: a0000001-0000-7000-8000-000000000001
# Länsen:   a0000002-0000-7000-8000-000000000002
```

---

### B. Demo SaaS-miljö (R-MT019)

**Approach:** Utöka `Run-DemoLocal.ps1` med en `-EnableMultitenancy` switch istället för att skapa ett nytt script. Det undviker att duplicera script-logiken.

```powershell
# Single-tenant demo (nuvarande beteende, oförändrat)
./scripts/Run-DemoLocal.ps1

# Multi-tenant demo (nytt)
./scripts/Run-DemoLocal.ps1 -EnableMultitenancy
```

Med `-EnableMultitenancy`:
- `Multitenancy__Enabled=true` sätts via miljövariabel
- `DevData__SaaSMode=true` aktiverar SaaS-seedning
- Databas default: `ConventionSystemSaaSDemo`
- Port default: `5100`

SaaS-seeden (samma som R-MT018) skapar Gammacon och Länsen med pre-seedade tenants.

Tillgängliga URL:er i SaaS-demo-läge:
- Portal-app: `http://localhost:5100/portal/` – logga in som `systemadmin@local.dev / Admin123!`
- Admin-app: `http://localhost:5100/admin/` med header `X-Tenant-ID: a0000001-0000-7000-8000-000000000001`
- Publik: `http://localhost:5100/` med header `X-Tenant-ID: a0000001-0000-7000-8000-000000000001`

För att använda admin- och publik-appen i demo-SaaS-läge via webbläsare behöver ett header-tillägg (t.ex. ModHeader för Chrome/Firefox) konfigureras med rätt `X-Tenant-ID`. Detta dokumenteras i DemoDeploy.md.

---

### C. Self-service signup (R-MT014)

#### Avgränsning

Self-service signup tillåter en ny organisatör att skapa ett konto utan att kontakta systemadmin. Det är en publik del av portal-appen.

#### Backend

Ny publik endpoint **utanför** `TenantResolutionMiddleware`-scope (annars 404 eftersom tenanten inte finns än):

```
POST /system/public/signup
```

Request:
```json
{
  "organizationName": "Sommarcon",
  "subdomain": "sommarcon",
  "adminName": "Anna Annasson",
  "adminEmail": "anna@sommarcon.se",
  "adminPassword": "Passw0rd!"
}
```

Ny query för realtidsvalidering (anropas med debounce från formuläret):
```
GET /system/public/subdomains/{subdomain}/availability
→ { "available": true }
```

Ny command `SignupTenantCommand` – skapar Tenant + Convention + admin-user i en transaktion och köar välkomstmail via outbox med inloggningslänk.

Felkoder:
- `invalid_subdomain` – format matchar inte `^[a-z0-9-]{3,63}$`
- `subdomain_already_taken` – subdomän finns redan
- `email_already_exists` – email finns redan för denna tenant

#### Frontend (portal-app)

Ny publik route `/signup` (ingen `authGuard`).

Formulärflöde i två steg:
1. **Steg 1 – Organisation:** organisationsnamn + subdomän (live-validering mot availability-endpoint med 400 ms debounce, visar check/error-ikon)
2. **Steg 2 – Administratör:** namn + email + lösenord + bekräfta lösenord

Bekräftelsesida efter lyckad signup: "Konto skapat – kolla din e-post för inloggningsinformation."

---

### D. Tenant-dashboard för tenant-ägare (R-MT015)

#### Auth-context i portal-appen

R-MT015 kräver att portal-appen stödjer **tenant-ägare** utöver systemadmins. En tenant-ägare loggar in via `/auth/login` med `X-Tenant-ID`-header (dev) eller subdomän (prod). JWT innehåller `tenant_id` och `is_admin`.

Portal-appen utökas med:
- Ny route `/tenant-login` – login-formulär för tenant-ägare (anropar `POST /auth/login` med `X-Tenant-ID`-header i dev)
- Ny guard `isTenantOwner` – kontrollerar `is_admin`-claim i tenant-JWT (ej systemadmin-JWT)
- Route `/tenant/dashboard` skyddad av `isTenantOwner`

I dev-läge: tenant-ägaren loggar in via `/tenant-login` med `X-Tenant-ID`-header i Angular environment.

#### Dashboard-innehåll (MVP)

| Sektion | Innehåll |
|---------|---------|
| Konventioner | Lista aktiva konventioner med direktlänk till admin-appen |
| Konto | Subdomän, organisationsnamn |
| Kontakt | Ändra kontaktperson och e-postadress |

Statistik, rapporter och fakturering är utanför scope för R-MT015.

---

## Motivering

### Varför deterministiska seed-UUIDs?

Fasta, kända tenant-IDs gör att `environment.development.ts` kan checkas in i repo:t med rätt värden. Utan detta måste varje utvecklare slå upp ID:t manuellt efter varje fresh seed – friktion som leder till att SaaS-läget undviks.

### Varför utöka Run-DemoLocal.ps1 snarare än nytt script?

All komplex script-logik (port-check, process-cleanup, log-tailing, wait-for-ready) finns på ett ställe. En `-EnableMultitenancy` switch är minimal förändring med maximal återanvändning.

### Varför publik signup-endpoint utanför middleware?

Middleware returnerar 404 för okänd subdomän. En ny tenant har per definition ingen subdomän registrerad än. Signup-endpointen måste ligga utanför middleware-scopet – precis som `POST /system/auth/login`.

### Varför stegvis formulär för signup?

Subdomänvalidering är asynkron och kräver nätverksanrop. Att dela formuläret i steg gör att användaren validerar subdomänen klart *innan* de fyller i övrig information.

---

## Bounded contexts som påverkas

| BC | Komponent | Förändring |
|----|-----------|-----------|
| Tenancy | `DevDataSeeder` | SaaS-gren med deterministiska tenants |
| Tenancy | `SignupTenantCommand` / handler | Ny publik provisioning |
| Tenancy | `CheckSubdomainAvailabilityQuery` | Ny query |
| Tenancy | `SystemPublicEndpoints` | Ny endpoint-grupp utanför middleware |
| Convention | Ingen förändring | Återanvänder `CreateConventionCommand` |
| Identity | Ingen förändring | Återanvänder befintlig registeringslogik |
| Frontend portal | `signup`, `tenant-login`, `tenant-dashboard` | Nya komponenter och routes |

---

## Risker

| Risk | Sannolikhet | Konsekvens | Hantering |
|------|-------------|------------|-----------|
| Deterministiska UUIDs krockar med existerande dev-databas | Låg | Seed-fel | Idempotent seed – kontrollera om tenant finns innan insert |
| Signup-endpoint missbrukas (spam) | Medel | Falska tenants | Rate limiting + email-verifiering (framtida fas) |
| Tenant-ägare i portal förväxlas med systemadmin | Medel | UI-förvirring | Separata login-routes och tydliga JWT-guards |
| SaaS-seeden är tung och saktar ner CI | Låg | Långsamma tester | Seed körs bara när `DevData:EnableSeeding=true`, aldrig i CI utan explicit flagga |

---

## Acceptanskriterier

### R-MT018 – Lokal SaaS-dev-workflow
- [ ] `dotnet run --environment SaaS` startar med `Multitenancy:Enabled=true`
- [ ] Seed skapar Gammacon och Länsen med deterministiska IDs
- [ ] `Run-SaaSLocal.ps1` startar API och seed körs automatiskt
- [ ] `ng serve admin` med Gammacon-ID i `environment.development.ts` träffar rätt tenant-data
- [ ] `ng serve admin` med Länsen-ID visar Länsen-data
- [ ] Tenant A kan inte se Tenant B:s data (verifierat i integrationstestfall)
- [ ] README innehåller instruktioner för SaaS-läge

### R-MT019 – Demo SaaS-miljö
- [ ] `./Run-DemoLocal.ps1 -EnableMultitenancy` startar SaaS-demo på port 5100
- [ ] Portal-appen är tillgänglig på `/portal/` och systemadmin kan logga in
- [ ] Admin-appen är tillgänglig med `X-Tenant-ID`-header för Gammacon
- [ ] Länsen-tenantens data är separerad och inte synlig i Gammacon-kontexten

### R-MT014 – Self-service signup
- [ ] `GET /system/public/subdomains/{subdomain}/availability` returnerar korrekt `available`
- [ ] `POST /system/public/signup` skapar Tenant + Convention + admin-user i en transaktion
- [ ] Välkomstmail skickas via outbox
- [ ] Portal-appen `/signup` renderar stegformulär utan inloggning
- [ ] Subdomän-fältet validerar live med debounce och visar check/error-ikon
- [ ] Fel subdomänformat ger 422 med `errorCode: invalid_subdomain`
- [ ] Dubbel subdomän ger 422 med `errorCode: subdomain_already_taken`

### R-MT015 – Tenant-dashboard
- [ ] Portal-appen `/tenant-login` renderar tenant-login-formulär
- [ ] Tenant-ägare kan logga in med `X-Tenant-ID`-header i dev
- [ ] `isTenantOwner`-guard blockerar access utan `is_admin`-claim i tenant-JWT
- [ ] Dashboard visar aktiva konventioner med länk till admin-appen
- [ ] Dashboard visar kontoinformation (subdomän, namn)

---

## Implementationsordning

```
R-MT018 + R-MT019  →  R-MT014  →  R-MT015
(SaaS dev + demo)    (Signup)    (Dashboard)
```

R-MT018 och R-MT019 implementeras i samma arbetspass eftersom de delar seeder-koden.
R-MT014 är backend-drivet och kan göras separat.
R-MT015 beror inte på R-MT014 men kräver ändring i portal-appens auth-flöde och bör komma sist.

---

Redo att bygga — godkänn med `/build`
