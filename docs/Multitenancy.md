# Multitenancy – Arkitektur, use cases och roadmap

Dokument som beskriver hur Conclave utökas från single-tenant (tenant-per-deploy) till att även stödja en SaaS-deploy med row-level multi-tenancy. De två modellerna koexisterar i samma kodbas.

---

## Strategi och avgränsning

### Två deploy-modeller

| Modell | Användare | Deploy | Tenancy |
|---|---|---|---|
| **Dedicated** | Stora konvent | En instans per konvent | Tenant-per-deploy (oförändrat) |
| **SaaS** | Små aktörer | Gemensam instans | Row-level tenancy |

Dedicated-deployn förändras inte. All ny kod är additiv och aktiveras av en feature-flagga i konfigurationen (`Multitenancy:Enabled`). En dedicated-deploy kör alltid med `false`.

### Vad som *inte* ingår i detta dokument

- Fakturering och betalplaner
- Self-service signup-portal (fas 4)
- SSO/SAML per tenant
- Dataexport per tenant

---

## Arkitektur

### Ny bounded context: `Tenancy`

Ett nytt bounded context ansvarar för tenant-livscykeln. Det lever utanför de fyra befintliga contexts och berörs inte av row-level-filtren (det måste kunna läsa alla tenants).

```
ConventionSystem.Domain/Tenancy/
  Tenant.cs                  # Aggregate root
  TenantId.cs                # Strong id: readonly record struct TenantId(Guid Value)
  TenantStatus.cs            # Active | Suspended | Deleted
  Events/
    TenantCreated.cs
    TenantSuspended.cs

ConventionSystem.Application/Tenancy/
  Commands/
    CreateTenantCommand.cs
    SuspendTenantCommand.cs
  Queries/
    GetTenantQuery.cs

ConventionSystem.Infrastructure/Tenancy/
  TenantRepository.cs
  TenantConfiguration.cs     # EF Core entity config

ConventionSystem.Api/Tenancy/
  TenantEndpoints.cs         # Endast tillgängliga med SystemAdmin-policy
```

### `Tenant`-aggregatet

```csharp
public sealed class Tenant : AggregateRoot
{
    public TenantId Id { get; private set; }
    public string Subdomain { get; private set; }   // "gammacon", "lansen"
    public string DisplayName { get; private set; }
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Skapas av systemadmin, inte via self-service (fas 1–3)
    internal Tenant(TenantId id, string subdomain, string displayName) { ... }

    public void Suspend() { ... }   // → TenantSuspended domain event
    public void Restore() { ... }
}
```

### Tenant-resolving – `ITenantContext`

Interface definieras i Application (eller Infrastructure), implementeras i Api.

```csharp
// Application/Tenancy/Abstractions/ITenantContext.cs
public interface ITenantContext
{
    TenantId TenantId { get; }
    bool IsResolved { get; }
}
```

```csharp
// Api/Middleware/TenantResolutionMiddleware.cs
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver)
    {
        TenantId? tenantId = null;

        // 1. Subdomän (produktion)
        tenantId = resolver.ResolveFromHost(context.Request.Host.Host);

        // 2. Header-fallback (development)
        if (tenantId is null && env.IsDevelopment())
        {
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var raw))
                tenantId = TenantId.Parse(raw!);
        }

        if (tenantId is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Items["TenantId"] = tenantId;
        await next(context);
    }
}
```

Middleware registreras **före** `UseAuthentication` och `UseAuthorization`.

### Global query filters i `AppDbContext`

```csharp
// Infrastructure/Persistence/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder builder)
{
    if (_multitenancyOptions.Enabled)
    {
        builder.Entity<Convention>()
            .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);

        // Samtliga rotentiteter per BC får samma filter
        // Event, Registration, Staff – alla aggregate roots
    }
}
```

Alla aggregate roots och entiteter med direkt tabell-mappning får en `TenantId`-kolumn. Den sätts automatiskt av en interceptor.

### `TenantSeedInterceptor`

Följer samma mönster som `EventDispatchInterceptor`.

```csharp
public sealed class TenantSeedInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        foreach (var entry in context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is ITenantScoped))
        {
            entry.Property("TenantId").CurrentValue = _tenantContext.TenantId;
        }
        return base.SavingChangesAsync(...);
    }
}
```

`ITenantScoped` är ett tomt markörgränssnitt som alla aggregate roots implementerar i SaaS-deployn.

### Feature-flagga

```json
// appsettings.json
{
  "Multitenancy": {
    "Enabled": false
  }
}

// appsettings.SaaS.json  (ASPNETCORE_ENVIRONMENT=SaaS)
{
  "Multitenancy": {
    "Enabled": true
  }
}
```

I `Program.cs`:

```csharp
if (builder.Configuration.GetValue<bool>("Multitenancy:Enabled"))
{
    builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
    builder.Services.AddScoped<TenantSeedInterceptor>();
    app.UseMiddleware<TenantResolutionMiddleware>();
}
else
{
    // Dedicated deploy: TenantContext är en no-op singleton
    builder.Services.AddSingleton<ITenantContext, SingleTenantContext>();
}
```

### Frontend – Angular

Multitenancy tillför en tredje app till frontend-monorepon:

| App | Port | Syfte | Tillgång |
|---|---|---|---|
| `admin` | 4200 | Konventionsadministration per tenant | `ConventionAdministrator` |
| `public` | 4201 | Besökarfrontend per tenant | Publik + inloggad |
| `portal` | 4202 | Systemadmin – tenant-provisioning | `SystemAdmin` |

`portal`-appen är fristående, lever på `system.conclave.se`, och använder samma `shared`-bibliotek som de andra apparna. Den autentiserar via systemadmin-login och har aldrig tillgång till tenant-scopad data.

En ny interceptor i `shared`-biblioteket, aktiv om `environment.multitenancy.enabled` är sant:

```typescript
// shared/interceptors/tenant-dev.interceptor.ts
export const tenantDevInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.production && environment.devTenantId) {
    req = req.clone({
      setHeaders: { 'X-Tenant-ID': environment.devTenantId }
    });
  }
  return next(req);
};
```

```typescript
// environment.development.ts
export const environment = {
  production: false,
  multitenancy: { enabled: true },
  devTenantId: 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
};
```

---

## Behörighetsmodell – tillägg

En ny systemövergripande roll tillkommer: `SystemAdmin`. Den existerar utanför tenant-scopet och kan hantera tenants.

| Roll | Scope | Kan |
|---|---|---|
| `SystemAdmin` | Hela systemet | Skapa/suspendera tenants, se alla tenants |
| `ConventionAdministrator` | En tenant | Som idag, men begränsad till sin tenant |

SystemAdmin-endpoints dekoreras med `.RequireAuthorization("IsSystemAdmin")` och kör utan global query filter (via en separat `ISystemDbContext` eller filter-bypass).

---

## Identity och användarhantering

### Designbeslut

Användare separeras fullständigt per tenant. `p@demo.se` kan registreras hos både Gammacon och Länsen – de är helt oberoende identiteter utan någon koppling till varandra. Systemadministratörer existerar utanför alla tenants och hanteras via en separat inloggningsväg.

Lösningen bygger på en enda `AspNetUsers`-tabell med en explicit `UserType`-kolumn (`TenantUser | SystemAdmin`) och `TenantId` som är NOT NULL för tenant-användare och NULL för systemadministratörer. Det globala unika indexet på email tas bort och ersätts med två separata constraints:

- Filtrerat unikt index på `(NormalizedEmail)` där `UserType = SystemAdmin`
- Filtrerat unikt index på `(NormalizedEmail, TenantId)` där `UserType = TenantUser`

`UserType` gör intentionen explicit i modellen och förhindrar att nullable `TenantId` används som implicit roll-indikator.

### `ApplicationUser`

```csharp
public sealed class ApplicationUser : IdentityUser
{
    public UserType UserType { get; set; }   // TenantUser | SystemAdmin
    public TenantId? TenantId { get; set; }  // NOT NULL om TenantUser, NULL om SystemAdmin
    public PersonId PersonId { get; set; }
}

public enum UserType { TenantUser, SystemAdmin }
```

### Identity-konfiguration i `OnModelCreating`

```csharp
builder.Entity<ApplicationUser>(b =>
{
    // Ta bort Identitys globala unika index på email och username
    b.HasIndex(u => u.NormalizedEmail).IsUnique(false);
    b.HasIndex(u => u.NormalizedUserName).IsUnique(false);

    // Unikt index för tenant-användare: email unik per tenant
    b.HasIndex(u => new { u.NormalizedEmail, u.TenantId })
        .IsUnique()
        .HasFilter("[UserType] = 0");  // 0 = TenantUser

    // Unikt index för systemadmins: email unik globalt
    b.HasIndex(u => u.NormalizedEmail)
        .IsUnique()
        .HasFilter("[UserType] = 1");  // 1 = SystemAdmin
});
```

Filtrerade index är SQL Server-syntax. EF Core genererar rätt SQL i migrationen.

### Tenant-scopad användarsökning

`UserManager.FindByEmailAsync` söker globalt och får aldrig användas direkt i SaaS-deployn. En `TenantAwareUserService` kapslar alla användarsökningar:

```csharp
public sealed class TenantAwareUserService(AppDbContext db)
{
    // Används vid login från tenant-subdomän
    public Task<ApplicationUser?> FindTenantUserAsync(
        string email, TenantId tenantId, CancellationToken ct = default) =>
        db.Users
            .Where(u => u.UserType == UserType.TenantUser
                     && u.NormalizedEmail == email.ToUpperInvariant()
                     && u.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);

    // Används vid login från system-ingången
    public Task<ApplicationUser?> FindSystemAdminAsync(
        string email, CancellationToken ct = default) =>
        db.Users
            .Where(u => u.UserType == UserType.SystemAdmin
                     && u.NormalizedEmail == email.ToUpperInvariant())
            .SingleOrDefaultAsync(ct);
}
```

De två metoderna är medvetet separata och korsar aldrig varandra. En tenant-login kan aldrig autentisera en systemadmin och vice versa.

### Separata login-ingångar

Tenant-användare och systemadministratörer loggar in via separata endpoints:

| Ingång | URL | Söker i |
|---|---|---|
| Tenant-login | `POST /auth/login` (på tenant-subdomän) | `TenantUser` med matchande `TenantId` |
| SystemAdmin-login | `POST /system/auth/login` | `SystemAdmin` (ingen tenant) |

Tenant-login-endpointen är registrerad i `TenantResolutionMiddleware`s scope och känner alltid till aktuell `TenantId`. SystemAdmin-login-endpointen är registrerad utanför middleware-scopet.

### JWT-claims

Tenant-användare:

```json
{
  "sub": "person-guid",
  "tenant_id": "tenant-guid",
  "user_type": "tenant_user",
  "is_admin": true
}
```

Systemadministratörer:

```json
{
  "sub": "person-guid",
  "user_type": "system_admin",
  "is_system_admin": true
}
```

`ICurrentUser` utökas med `UserType` och `TenantId?`. Handlers som kräver tenant-scope validerar att `ICurrentUser.TenantId == ITenantContext.TenantId` – mismatch ger 403. Detta är ett extra säkerhetslager utöver middleware-resolvingen.

### Tenant-isolering av `Person`

`Person`-entiteten i Convention-BC är redan tenant-isolerad via `TenantId` på tabellen (R-MT002). `ApplicationUser.PersonId` pekar alltid på en `Person` inom samma tenant. En systemadmin har ingen `Person` i konvent-domänen – de opererar direkt via sina egna endpoints utan `PersonId`-koppling.

---

## Databasmigrering

Vid övergång från dedicated till SaaS-schema körs en EF Core-migration som:

1. Lägger till `Tenants`-tabell
2. Lägger till `TenantId`-kolumn (nullable → NOT NULL efter backfill) på alla berörda tabeller
3. Skapar index på `TenantId` + primärnyckel för alla tabeller

För en ny SaaS-deploy körs migrationen från start. Dedicated-deployer berörs inte.

---

## Use cases

### UC-MT001 – Skapa tenant (manuell, systemadmin)

**Aktör:** SystemAdmin  
**Förutsättning:** Multitenancy aktiverat, systemadmin inloggad

**Flöde:**
1. SystemAdmin skickar `POST /system/tenants` med subdomän och visningsnamn
2. Systemet validerar att subdomänen är unik och följer format (`[a-z0-9-]+`)
3. `Tenant`-aggregat skapas med status `Active`
4. `TenantCreated`-event dispatkas
5. Systemet returnerar `TenantId`

**Acceptanskriterier:**
- Subdomän får inte redan finnas
- Subdomän valideras mot regex `^[a-z0-9-]{3,63}$`
- Skapande utan `ConventionId` – konvent skapas separat av den nya tenantens admin

---

### UC-MT002 – Lös upp tenant från request

**Aktör:** Systemet (middleware)  
**Förutsättning:** Request inkommer mot SaaS-deploy

**Flöde:**
1. Middleware extraherar host-header
2. I produktion: subdomän parsas ur host (`gammacon.conclave.se` → `gammacon`)
3. I development: `X-Tenant-ID`-header används som fallback
4. Tenant slås upp mot `Tenants`-tabell
5. `TenantId` sätts i `HttpContext.Items`
6. Suspended tenant returnerar 403 med `errorCode: tenant_suspended`
7. Okänd tenant returnerar 404

**Acceptanskriterier:**
- `X-Tenant-ID`-header ignoreras i produktion (ej development-miljö)
- Tenant med status `Suspended` ger 403, inte 404
- Varje request validerar mot databasen (cachas med kort TTL, se R-MT005)

---

### UC-MT003 – Suspendera tenant

**Aktör:** SystemAdmin  
**Förutsättning:** Tenant existerar med status `Active`

**Flöde:**
1. SystemAdmin skickar `PUT /system/tenants/{tenantId}/suspend`
2. `Tenant.Suspend()` anropas
3. `TenantSuspended`-event dispatkas
4. Efterföljande requests mot tenantens subdomän returnerar 403

**Acceptanskriterier:**
- Aktiva sessioner avbryts inte omedelbart – JWT-tokens gäller till expiry
- Redan suspended tenant ger domainrule-violation

---

### UC-MT004 – Återaktivera tenant

**Aktör:** SystemAdmin  
**Förutsättning:** Tenant med status `Suspended`

**Flöde:**
1. SystemAdmin skickar `PUT /system/tenants/{tenantId}/restore`
2. `Tenant.Restore()` anropas
3. Tenant returnerar till `Active`

---

### UC-MT005 – Registrera tenant-användare

**Aktör:** Tenant-admin  
**Förutsättning:** Tenant aktiv, inloggad som `ConventionAdministrator`

**Flöde:**
1. Admin skickar `POST /auth/register` med email och lösenord på tenant-subdomän
2. Middleware har redan resolvar `TenantId` från subdomänen
3. Systemet kontrollerar att email inte redan finns för denna tenant
4. `ApplicationUser` skapas med `UserType = TenantUser` och korrekt `TenantId`
5. `Person`-entitet skapas i Convention-BC med samma `TenantId`
6. `ApplicationUser.PersonId` kopplas till den nya `Person`

**Acceptanskriterier:**
- Samma email kan registreras hos olika tenants utan konflikt
- Samma email hos samma tenant ger 422 med `errorCode: email_already_exists`
- `TenantId` tas aldrig från request-body – alltid från middleware

---

### UC-MT006 – Logga in som tenant-användare

**Aktör:** Tenant-användare  
**Förutsättning:** Användare registrerad hos tenanten

**Flöde:**
1. Användare skickar `POST /auth/login` med email och lösenord på tenant-subdomän
2. `TenantAwareUserService.FindTenantUserAsync(email, tenantId)` anropas
3. Lösenord verifieras
4. JWT utfärdas med `tenant_id`, `user_type: tenant_user` och relevanta rollclaims
5. Token returneras

**Acceptanskriterier:**
- Felaktigt lösenord ger 401 – aldrig information om huruvida emailen finns
- En systemadmins email går inte att logga in med via tenant-endpointen, även om emailen råkar matcha
- Token innehåller alltid `tenant_id` för tenant-användare

---

### UC-MT007 – Logga in som systemadmin

**Aktör:** SystemAdmin  
**Förutsättning:** SystemAdmin-användare skapad manuellt i databasen

**Flöde:**
1. Admin skickar `POST /system/auth/login` med email och lösenord
2. Endpointen ligger utanför `TenantResolutionMiddleware`s scope
3. `TenantAwareUserService.FindSystemAdminAsync(email)` anropas
4. JWT utfärdas med `user_type: system_admin` och `is_system_admin: true`
5. Token returneras

**Acceptanskriterier:**
- Endpointen är inte nåbar via tenant-subdomän – endast via system-ingången
- En tenant-användares email går inte att logga in med via systemadmin-endpointen
- SystemAdmin-token innehåller aldrig `tenant_id`

---

### UC-MT008 – Provisionera konvent för ny tenant

**Aktör:** SystemAdmin (fas 1–3), Tenant-admin (fas 4 – self-service)  
**Förutsättning:** Tenant skapad (UC-MT001)

**Flöde:**
1. Aktör skickar `POST /conventions` med `TenantId` i JWT eller header
2. `Convention`-aggregat skapas med korrekt `TenantId`
3. En `Person` skapas och tilldelas rollen `ConventionAdministrator`
4. Returnerar `ConventionId`

**Notering:** Flödet återanvänder befintlig `CreateConventionCommand`. Det enda som tillkommer är att `TenantId` sätts av `TenantSeedInterceptor`.

---

## Testplan

### Enhetstester

| Test | Vad som verifieras |
|---|---|
| `TenantResolutionMiddleware` – subdomän finns | Korrekt `TenantId` sätts i context |
| `TenantResolutionMiddleware` – subdomän saknas, dev, header finns | Header-värde används |
| `TenantResolutionMiddleware` – subdomän saknas, prod, header finns | Header ignoreras, 404 returneras |
| `TenantResolutionMiddleware` – tenant suspended | 403 returneras |
| `Tenant.Suspend()` – redan suspended | `DomainRuleViolationException` kastas |
| `TenantAwareUserService` – samma email, olika tenants | Båda hittas, ingen konflikt |
| `TenantAwareUserService` – tenant-login med systemadmin-email | Returnerar null |
| `TenantAwareUserService` – systemadmin-login med tenant-email | Returnerar null |

### Integrationstester (Testcontainers)

Kritiskt: verifiera att tenant A **aldrig** kan se tenant B:s data.

```csharp
[Fact]
public async Task TenantA_CannotSee_TenantBData()
{
    // Arrange: skapa Convention för tenant A och tenant B
    // Act: hämta conventions med tenant A:s context
    // Assert: endast tenant A:s convention returneras
}
```

Tester körs med `ASPNETCORE_ENVIRONMENT=SaaS` för att aktivera multitenancy-flaggan.

---

## Roadmap

### Fas 1 – Infrastruktur (R-MT001–R-MT004)

Förutsättning för allt annat. Inga use cases är synliga för slutanvändare.

**R-MT001** – `Tenancy` bounded context  
Skapa `Tenant`-aggregat, `TenantId`, `TenantStatus`, domain events. Inga handlers än.

**R-MT002** – EF Core: `TenantId` på alla tabeller + global query filter  
Migration, `ITenantContext`, `TenantSeedInterceptor`. Feature-flagga styr om filter aktiveras.  
*Kritiskt: skriv isolationstest innan denna mergas.*

**R-MT003** – Middleware: tenant-resolving  
`TenantResolutionMiddleware` med subdomän + header-fallback. Enhetstester för prioritetsordning.

**R-MT004** – `SystemAdmin`-roll och policy  
Ny claim, ny policy, registrera i `AuthorizationOptions`. Endpoints för tenant-CRUD.

---

### Fas 2 – Use cases och API (R-MT005–R-MT009)

**R-MT005** – Identity: `ApplicationUser` med `UserType` och filtrerade index  
Migration som tar bort globalt email-index och lägger till filtrerade composite-index. `TenantAwareUserService`. Enhetstester för sökvägsisolering.

**R-MT006** – UC-MT001, UC-MT003, UC-MT004: Skapa/suspendera/återaktivera tenant  
Handlers, endpoints, integrationstester.

**R-MT007** – UC-MT002: Tenant-resolving med caching  
Kort TTL-cache (60 s) på tenant-lookup för att undvika databasanrop per request. Invalideras vid suspend/restore via domain event.

**R-MT008** – UC-MT005, UC-MT006, UC-MT007: Registrering och login  
Separata endpoints för tenant-login och systemadmin-login. JWT-claims uppdateras med `user_type` och `tenant_id`.

**R-MT009** – UC-MT008: Provisionering av konvent och admin-användare  
Återanvänder `CreateConventionCommand`. Lägg till provisioneringsscript/endpoint för manuell setup.

---

### Fas 3 – Frontend (R-MT010–R-MT012)

**R-MT010** – `tenantDevInterceptor` i shared-biblioteket  
Aktiv om `environment.multitenancy.enabled && !production`. Läser `devTenantId` från environment.

**R-MT011** – `portal`-app: grundstruktur  
Ny Angular-app i monorepon. Egen routing, egen `environment`-konfiguration, systemadmin-autentisering via `/system/auth/login`. Guard som blockerar åtkomst utan `is_system_admin`-claim. Ingen tenant-interceptor.

**R-MT012** – `portal`-app: tenant-hantering  
Lista tenants med status, skapa tenant (subdomän + visningsnamn), suspendera/återaktivera. Följer samma listnings- och formulärmönster som admin-appen: page-header → action-bar → inline create-card → data-table.

---

### Fas 4 – Provisioning och self-service (R-MT013–R-MT017)

**R-MT013** – `portal`-app: provisioneringsvy (systemadmin)  
Formulär för att provisionera konvent och admin-användare åt en ny tenant. Anropar provisionerings-API (UC-MT008). Visar provisioneringsstatus och kopierbara inloggningsuppgifter för ny tenant-admin.

**R-MT014** – `portal`-app: self-service signup  
Publik del av portal-appen (ingen inloggning krävs). Formulär: organisationsnamn, önskad subdomän, kontaktperson. Anropar signup-API som skapar tenant och skickar välkomstmail. Beror på att R-MT009 är klart.

**R-MT015** – `portal`-app: tenant-dashboard (self-service)  
Inloggad vy för tenant-ägare. Konventionsöversikt, kontoinställningar, kontaktinformation. Autentiserar med tenant-ägar-roll, inte `SystemAdmin`.

**R-MT016** – Välkomstmail vid provisioning  
E-post med inloggningslänk och temporärt lösenord skickas till ny tenant-admin. Beror på att e-postinfrastruktur är definierad.

**R-MT017** – Faktureringsintegration  
Stripe eller motsvarande. Utanför scope för detta dokument.

---

## Beroenden till befintlig roadmap

Multitenansy-arbetet är oberoende av R16–R18 (biljettflödet) och kan köras parallellt. R-MT001–R-MT002 bör dock ske i ett dedikerat arbetspass eftersom de rör `AppDbContext` och migrations – samma filer som biljettimplementationen rör.

Rekommenderad ordning om båda spåren körs:

1. Slutför R16–R18 (biljett)
2. Påbörja R-MT001 (Tenancy-context är isolerat)
3. R-MT002 i eget PR – databas-migrationen är den enda risken för konflikt
