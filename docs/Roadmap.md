# Roadmap – Conclave

Dokument för att spåra vad som är klart och vad som återstår inför produktionsstart och vidare.

---

## Nuläge (april 2026)

### Klar – backend-grund
- **Domänmodell** – alla fyra bounded contexts implementerade (Convention, Event, Registration, Staff) med aggregate roots, entiteter, value objects och domain events
- **CQRS-hanterare** – commands och queries för alla use cases (UC001–UC002b, UC003–UC012, UC-ST001–ST006, UC-TK001–TK004, UC-VR001–VR003, UC-SA001–SA007, UC-SR001–SR002, UC-EV001–EV011)
- **Infrastruktur** – EF Core med tre databaser (konventionsdatabas, systemdatabas, identitetsdatabas), EventDispatchInterceptor, DomainEventLog
- **Auth-stack** – JWT-middleware, ASP.NET Identity, `POST /auth/login`, tenant-resolution via `X-Convention-Id`-header
- **UC002** – identifiera eller skapa person vid inloggning
- **Minimal API** – endpoints för alla ovanstående use cases

### Klar – Fas 1 (end-to-end)
- **1.1 Tenant-provisionering** – `POST /system/conventions` skapar konvention i ConventionDb, tenant-post i SystemDb, `ApplicationUser` i IdentityDb och `ConventionUserLink`
- **1.2 Profilkomplettering** – `PUT /me/profile` låter inloggad användare uppdatera namn, e-post och telefon
- **1.3 Rollbaserad auktorisering** – `is_admin`-claim i JWT, `IsAdmin`-policy, admin-endpoints skyddade; domänägarskapskontroller görs inline i handlers
- **1.4 Global felhantering** – `GlobalExceptionHandler` med ProblemDetails (RFC 7807): `ArgumentException` → 400, `InvalidOperationException` → 422, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404

### Klar – Fas 2
- **2.1 E-postnotifikationer** – `IEmailService` med handlers för `VisitorRegistrationConfirmed`, `StaffApplicationReceived/Accepted/Rejected`, `VersionApproved/Rejected`; `LoggingEmailService` som platshållare tills SMTP/SendGrid kopplas in
- **2.2 Publik feed-API** – `GET /feed/editions/{id}` och `GET /feed/events/{id}`, anonyma, filtrerar bort intern data
- **2.3 Integrationstester** – 14 tester mot SQL Server (Testcontainers), täcker tenant-resolution, UC002, auth-flödet och publik feed; per-test isolerade databaser via `ProvisionAsync`

### Klar – Fas 3 (delvis)
- **3.0 Workspace och delad infrastruktur** – Angular monorepo med admin-app, publik-app och delat bibliotek; `AuthService` (signals), `authGuard`, `adminGuard`, `ConventionInterceptor`, `AuthInterceptor`, alla API-modeller
- **3.1.1 Scaffold och layout** – App-shell med sidenav, toolbar, logout; lazy-loadade routes med `authGuard` + `adminGuard`
- **3.1.2 Inloggning** – Inloggningsformulär med Angular Material Reactive Forms, JWT sparas i sessionStorage, redirect vid lyckad inloggning, logout
- **API-förbättringar** – CORS-policy för Angular-apparna, SystemDb/IdentityDb auto-migreras vid uppstart, ConventionDb auto-migreras vid provisioning

### Ej klar
Se faserna nedan.

---

## ~~Fas 1 – Systemet fungerar end-to-end~~ ✓ Klar

*Alla delar implementerade – se "Nuläge" ovan.*

---

## ~~Fas 2 – Operativa funktioner~~ ✓ Klar

### ~~2.1 E-postnotifikationer~~ ✓ Klar
### ~~2.2 Publik feed-API~~ ✓ Klar
### ~~2.3 Integrationstester~~ ✓ Klar

---

## Fas 3 – Frontend

Två Angular-appar med en delad kodbas för API-typer och tjänster. Hanteras som ett Angular-workspace (monorepo) under `frontend/`.

### Arkitekturella beslut

| Beslut | Val | Motivering |
|--------|-----|------------|
| Workspace | Angular monorepo (en workspace, två appar + ett bibliotek) | Delar API-typer, interceptors och auth-tjänst |
| UI-komponenter | Angular Material | Vältestat, tillgänglighetsanpassat, snabb development |
| Styling | Angular Material theming + SCSS | Material för admin, konventionsthema via CSS-variabler för publik vy |
| State | Angular Signals + services | Tillräckligt för MVP, undviker NgRx-overhead |
| Forms | Reactive Forms | Bättre kontroll och validering |
| HTTP | Angular HttpClient med interceptors | Centraliserad header-hantering |
| Routing | Standalone components, lazy-loaded feature-moduler | Modern Angular-stil, snabbare initial laddning |

```
frontend/
├── projects/
│   ├── admin/          # Admin-app (port 4200)
│   ├── public/         # Publik vy (port 4201)
│   └── shared/         # Bibliotek: API-typer, tjänster, interceptors, guards
├── angular.json
└── package.json
```

### Tenant-kontext i Angular

Konventions-ID konfigureras per driftsättning via `environment.ts`. HTTP-interceptorn lägger automatiskt till `X-Convention-Id`-headern på alla anrop. Den publika appen deployas en gång per konvention med rätt ID inbakat.

> **OBS – måste lösas inför produktion:** Nuvarande modell kräver en unik deploy per konvention enbart för att byta `conventionId`. Se teknisk skuld: *Tenant-routing via domän*.

---

### ~~Fas 3.0 – Workspace och delad infrastruktur~~ ✓ Klar

*Förutsättning för båda apparna. Byggs en gång.*

**Struktur och tooling**
- Angular workspace: `ng new conclave-web --no-create-application`
- Admin-app: `ng generate application admin`
- Publik-app: `ng generate application public`
- Delat bibliotek: `ng generate library shared`
- Angular Material installeras i båda apparna

**Delat bibliotek (`projects/shared/`)**

| Del | Innehåll |
|-----|---------|
| `api/models/` | TypeScript-interface för alla API-svar (EditionDto, EventDto, PersonDto etc.) |
| `api/services/` | Injectable-tjänster som wrappar varje endpoint-grupp (AuthService, EditionService, EventService etc.) |
| `interceptors/` | `ConventionInterceptor` (lägger till X-Convention-Id), `AuthInterceptor` (lägger till Bearer token) |
| `auth/` | `AuthService`: login, logout, tokenlagring (sessionStorage), JWT-parsing, `isAdmin$` signal |
| `guards/` | `authGuard` (kräver inloggning), `adminGuard` (kräver `is_admin`-claim) |
| `environment/` | Typade miljövariabler inkl. `apiBaseUrl` och `conventionId` |

---

### Fas 3.1 – Admin-app

Rollbaserad app för konventionsadministratörer. Kräver `is_admin`-claim.

#### ~~3.1.1 Scaffold och layout~~ ✓ Klar
- ~~App-shell: topbar, sidebar-navigation, content-area~~
- ~~Routing: `AdminGuard` på alla routes utom login~~
- Felsidor: 401, 403, 404 *(ej klar)*
- ~~Lazy-loaded feature-routes per sektion~~

#### ~~3.1.2 Inloggning~~ ✓ Klar
- ~~Inloggningsformulär (`POST /auth/login`)~~
- ~~Token sparas, `is_admin`-check, redirect till dashboard~~
- ~~Logout rensar token och navigerar till login~~

#### ~~3.1.3 Dashboard~~ ✓ Klar
- ~~Välkomstsida med konventionsnamn och upplageöversikt~~
- ~~Upplagor visas med status och datum~~
- ~~Kräver:~~ `GET /conventions/{id}`, `GET /conventions/{id}/editions`

#### 3.1.4 Konventionsstruktur
Upplaga, lokaler, funktionsområden, stationer, kategorier:

| Skärm | Endpoints |
|-------|-----------|
| Upplageöversikt (lista + skapa) | `GET /conventions/{id}/editions`, `POST /conventions/{id}/editions` |
| Upplagestatus + publicering | `GET /editions/{id}`, `POST /editions/{id}/publish` |
| Öppna registrering | `POST /editions/{id}/registrations/{type}/open` |
| Lokaler (lista + skapa) | `POST /editions/{id}/venues` |
| Funktionsområden + stationer | `POST /editions/{id}/staff-areas`, `POST /editions/{id}/stations` |
| Kategorier (lista + ansvarig) | `POST /editions/{id}/categories`, `PUT /editions/{id}/categories/{id}` |

#### 3.1.5 Personregister
- Personlista med sökning
- Skapa, redigera, avaktivera person
- *Kräver ny backend-endpoint:* `GET /editions/{id}/persons`

#### 3.1.6 Evenemangs-granskning
- Lista evenemang under granskning (`GET /editions/{id}/events`, filtrera på status `UnderReview`)
- Detaljvy: titel, beskrivning, sessionönskemål, kommentarer
- Åtgärder: godkänn (`POST /events/{id}/approve`) eller avvisa med kommentar (`POST /events/{id}/reject`)
- Visa publicerade evenemang och schemalägg sessioner (`POST /events/{id}/sessions`)

#### 3.1.7 Bemanningshantering
- Passöversikt per station: `GET /stations/{id}/shifts`
- Skapa pass, tilldela/bekräfta/avslå tilldelningar
- Staffansökanslista: acceptera/avslå
- *Kräver ny backend-endpoint:* `GET /editions/{id}/staff-applications`

#### 3.1.8 Registreringsöversikt
- Biljettyper: skapa, visa
- Besökarregistreringar: lista, bekräfta betalning, makulera biljett
- *Kräver ny backend-endpoint:* `GET /editions/{id}/visitor-registrations`, `GET /editions/{id}/ticket-types`

---

### Fas 3.2 – Publik vy

Konventionsstyld app för besökare, staff och arrangörer. Deployed en gång per konvention.

#### 3.2.1 Scaffold och layout
- App-shell: header med konventionslogotyp, navigation, footer
- Konventionsthema via CSS-variabler (primärfärg, logotyp – konfigurerbart i environment)
- Publika routes (ej inloggad), privata routes (inloggad)

#### 3.2.2 Inloggning och profilkomplettering
- Inloggningsformulär (`POST /auth/login`)
- Profilvy: visa och uppdatera namn/e-post/telefon
- *Kräver ny backend-endpoint:* `GET /me/profile`

#### 3.2.3 Evenemangslista och detaljvy
- Upplageöversikt med kategorier och sökfilter (från `GET /feed/editions/{id}`)
- Evenemangsdetaljsida (från `GET /feed/events/{id}`)
- Sessionsschema med tid, lokal och platsstatus

#### 3.2.4 Besökarregistrering
- Registreringsformulär (`POST /editions/{id}/visitor-registrations`)
- Betalningstatus och bekräftelse
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-visitor-registration`

#### 3.2.5 Staffansökan
- Ansökningsformulär: tillgänglighet och stationsönskemål
- Endpoints: `POST /editions/{id}/staff-applications`, tillgänglighets- och önskemålsendpoints
- Ansökningsstatus (accepterad/avslagen)
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-staff-application`

#### 3.2.6 Arrangörsflöde
- Skapa event (`POST /editions/{id}/events`)
- Redigera utkast, lägg till sessionönskemål och medarrangörer
- Skicka in för granskning (`POST /events/{id}/submit`)
- Status och kommentarer på inskickade event
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-events`

#### 3.2.7 Sessionsregistrering
- Anmäl till en session (`POST /sessions/{id}/registrations`)
- Avboka (`DELETE /session-registrations/{id}`)
- Visa egna registreringar

---

### Backend-komplement som krävs under Fas 3

Dessa GET-queries saknas i dagsläget. Byggs precis innan den frontendsektion som behöver dem.

| Endpoint | Krävs för | Auth |
|----------|-----------|------|
| `GET /me/profile` | 3.2.2 profilvy | Autentiserad |
| `GET /editions/{id}/persons` | 3.1.5 personregister | IsAdmin |
| `GET /editions/{id}/staff-applications` | 3.1.7 bemanningshantering | IsAdmin |
| `GET /editions/{id}/visitor-registrations` | 3.1.8 registreringsöversikt | IsAdmin |
| `GET /editions/{id}/ticket-types` | 3.1.8 biljettyper | Publik |
| `GET /editions/{id}/my-visitor-registration` | 3.2.4 besökarregistrering | Autentiserad |
| `GET /editions/{id}/my-staff-application` | 3.2.5 staffansökan | Autentiserad |
| `GET /editions/{id}/my-events` | 3.2.6 arrangörsflöde | Autentiserad |

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| **Skydda provisioning-endpoint** | `POST /system/conventions` är oskyddad – vem som helst kan skapa tenants och databaser. Måste skyddas med API-nyckel eller system-admin-roll innan produktion. | **Hög – blockar produktion** |
| **Tenant-routing via domän** | Idag: `conventionId` hårdkodat i `environment.ts` → unik deploy per konvention. Ska vara: TenantMiddleware löser tenant från HTTP-domän (subdomän); frontend resolvar `conventionId` dynamiskt från API:t baserat på `window.location.hostname`. Tenant-tabellen har redan ett `Domain`-fält. | **Hög – blockar produktion** |
| **Skalbart val av ansvariga personer** | När personlistan växer behövs en bättre lösning än enkel dropdown för att välja ansvariga (t.ex. sökbar/autocomplete-väljare, filtrering på aktiv status och begränsning till relevanta kandidater). Utvärdera även om en särskild roll ska krävas för att kunna tilldelas som ansvarig. | Medel |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person (admin-väg och auth-väg). Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande | Medel – se till att UC002-vägen aldrig kolliderar |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person+länk simultaneously | Låg – unikt index är sista skyddet |

---

## Nästa konkreta steg (förslag)

1. **Fas 3.1.4** – Konventionsstruktur (Edition, Venue, Area, Station, Category)
2. **Fas 3.1.5–3.1.8** – Personregister, event-granskning, bemanning, registrering
3. **Fas 3.2** – Publik vy
4. **Pre-produktion** – Skydda provisioning-endpoint + domänbaserad tenant-routing
