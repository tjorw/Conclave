# Roadmap – Conclave

Dokument för att spåra vad som är klart och vad som återstår inför produktionsstart och vidare.

---

## Nuläge (april 2026)

### Klar – backend-grund
- **Domänmodell** – alla fyra bounded contexts implementerade (Convention, Event, Registration, Staff) med aggregate roots, entiteter, value objects och domain events
- **CQRS-hanterare** – commands och queries för alla use cases (UC001–UC002b, UC003–UC012, UC-ST001–ST006, UC-TK001–TK004, UC-VR001–VR003, UC-SA001–SA007, UC-SR001–SR002, UC-EV001–EV011)
- **Infrastruktur** – EF Core med en databas per deploy (`dbo`-schema för domändata, `identity`-schema för ASP.NET Identity), EventDispatchInterceptor, DomainEventLog
- **Auth-stack** – JWT-middleware, ASP.NET Identity, `POST /auth/login`
- **UC002** – identifiera eller skapa person vid inloggning (`PersonId` direkt på `ApplicationUser`)
- **Minimal API** – endpoints för alla ovanstående use cases

### Klar – Fas 1 (end-to-end)
- **1.1 Konventionsinitiering** – `CreateConventionCommand` skapar konvention och admin-person; `ApplicationUser` med `PersonId` skapas via `UserManager` vid seeding/onboarding
- **1.2 Profilkomplettering** – `PUT /me/profile` låter inloggad användare uppdatera namn, e-post och telefon
- **1.3 Rollbaserad auktorisering** – `is_admin`-claim i JWT, `IsAdmin`-policy, admin-endpoints skyddade; domänägarskapskontroller görs inline i handlers
- **1.4 Global felhantering** – `GlobalExceptionHandler` med ProblemDetails (RFC 7807): `ArgumentException` → 400, `InvalidOperationException` → 422, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404

### Klar – Fas 2
- **2.1 E-postnotifikationer** – `IEmailService` med handlers för `VisitorRegistrationConfirmed`, `StaffApplicationReceived/Accepted/Rejected`, `VersionApproved/Rejected`; `LoggingEmailService` som platshållare tills SMTP/SendGrid kopplas in
- **2.2 Publik feed-API** – `GET /feed/editions/{id}` och `GET /feed/events/{id}`, anonyma, filtrerar bort intern data
- **2.3 Integrationstester** – tester mot SQL Server (Testcontainers), täcker UC002 (first login), auth-flödet och publik feed; delad konvention per testklass, isolering via unika testkonton

### Klar – Fas 3 (delvis)
- **3.0 Workspace och delad infrastruktur** – Angular monorepo med admin-app, publik-app och delat bibliotek; `AuthService` (signals), `authGuard`, `adminGuard`, `AuthInterceptor`, alla API-modeller
- **3.1.1 Scaffold och layout** – App-shell med sidenav, toolbar, logout; lazy-loadade routes med `authGuard` + `adminGuard`; felsidor 401, 403, 404; `httpErrorInterceptor` omdirigerar API-401 till `/unauthorized`
- **3.1.2 Inloggning** – Inloggningsformulär med Angular Material Reactive Forms, JWT sparas i sessionStorage, redirect vid lyckad inloggning, logout
- **API-förbättringar** – CORS-policy för Angular-apparna, ConventionDbContext + ApplicationIdentityDbContext auto-migreras vid uppstart
- **3.1.4 Konventionsstruktur** – Upplagehantering, lokaler, funktionsområden, kategorier med full CRUD; aktiv upplaga i sessionStorage-kontext; tabbar och tabelllistningar
- **3.1.5 Personregister** – Personlista med sökning, skapa/redigera/avaktivera/återaktivera; admin-flagga; standardmönster för listningssidor dokumenterat
- **3.1.9 Kontohantering** – `hasAccount`/`isLocked` i PersonDto, skicka återställningslänk, lås/lås upp konto
- **3.1.10 Rollvyer per upplaga** – `GET /editions/{id}/visitors|organisers|staff|responsibles`; fyra nya flikar i upplage-detaljvyn (Besökare, Arrangörer, Funktionärer, Ansvariga)
- **3.1.7 Bemanningshantering** – Passöversikt per station, skapa/ställa in pass, tilldela/bekräfta/avslå/avboka tilldelningar, staffansökningslista med acceptera/avslå; `GET /editions/{id}/staff-applications` implementerad
- **3.2.1 Publik scaffold och layout** – ShellComponent med brandad topnav, CSS custom properties, lazy-loadade routes, `EditionService` med APP_INITIALIZER
- **3.2.2 Hem och program** – Hemsida med hero och CTA-kort, programlista med dagsfilter och kategori-chips, evenemangsdetaljvy med sessionsexpandering; aktiv upplaga styrs av admin via `POST /editions/{id}/set-active`

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

### Konventions-ID i Angular

Konventions-ID konfigureras per driftsättning via `environment.ts`. Det används för att konstruera URL:er till feed-endpointarna (`/feed/{conventionId}/...`). Varje konvention är en separat deploy – ingen delad infrastruktur.

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
| `interceptors/` | `ConventionInterceptor` (används för feed-URL-prefix med `conventionId`), `AuthInterceptor` (lägger till Bearer token) |
| `auth/` | `AuthService`: login, logout, tokenlagring (sessionStorage), JWT-parsing, `isAdmin$` signal |
| `guards/` | `authGuard` (kräver inloggning), `adminGuard` (kräver `is_admin`-claim) |
| `environment/` | Typade miljövariabler inkl. `apiBaseUrl` och `conventionId` |

---

### Fas 3.1 – Admin-app

Rollbaserad app för konventionsadministratörer. Kräver `is_admin`-claim.

#### ~~3.1.1 Scaffold och layout~~ ✓ Klar
- ~~App-shell: topbar, sidebar-navigation, content-area~~
- ~~Routing: `AdminGuard` på alla routes utom login~~
- ~~Felsidor: 401, 403, 404~~
- ~~Lazy-loaded feature-routes per sektion~~

#### ~~3.1.2 Inloggning~~ ✓ Klar
- ~~Inloggningsformulär (`POST /auth/login`)~~
- ~~Token sparas, `is_admin`-check, redirect till dashboard~~
- ~~Logout rensar token och navigerar till login~~

#### ~~3.1.3 Dashboard~~ ✓ Klar
- ~~Välkomstsida med konventionsnamn och upplageöversikt~~
- ~~Upplagor visas med status och datum~~
- ~~Kräver:~~ `GET /conventions/{id}`, `GET /conventions/{id}/editions`

#### ~~3.1.4 Konventionsstruktur~~ ✓ Klar
~~Upplaga, lokaler, funktionsområden, stationer, kategorier:~~

| ~~Skärm~~ | ~~Endpoints~~ |
|-------|-----------|
| ~~Upplageöversikt (lista + skapa)~~ | ~~`GET /conventions/{id}/editions`, `POST /conventions/{id}/editions`~~ |
| ~~Upplagestatus + publicering~~ | ~~`GET /editions/{id}`, `POST /editions/{id}/publish`~~ |
| ~~Öppna registrering~~ | ~~`POST /editions/{id}/registrations/{type}/open`~~ |
| ~~Lokaler (lista + skapa + redigera + ta bort)~~ | ~~`POST/PUT/DELETE /editions/{id}/venues/{id}`~~ |
| ~~Funktionsområden + stationer~~ | ~~`POST/PUT/DELETE /editions/{id}/staff-areas/{id}`~~ |
| ~~Kategorier (lista + CRUD)~~ | ~~`POST/PUT/DELETE /editions/{id}/categories/{id}`~~ |

#### ~~3.1.5 Personregister~~ ✓ Klar
- ~~Personlista med sökning~~
- ~~Skapa, redigera, avaktivera och återaktivera person~~
- ~~`GET /conventions/{id}/persons`, admin-flagga via join mot `convention_administrators`~~

#### ~~3.1.6 Evenemangs-granskning~~ ✓ Klar
- ~~Lista evenemang med statusfilter (`GET /editions/{id}/events`)~~
- ~~Skapa evenemang (kategori + arrangör), ställ in från lista~~
- ~~Detaljvy: titel, beskrivning, sessionönskemål~~
- ~~Redigera utkastfält och sessionönskemål~~
- ~~Åtgärder: godkänn / avvisa med kommentar / ställ in / skicka in för granskning~~

#### 3.1.6b Evenemangsflöde – genomgång och förfining
Mål: beskriva och införa ett komplett samarbetsflöde mellan arrangör (publik app) och admin (admin-app), från utkast till publicerat schema och vidare till hantering av ändringskommentarer.

**Flöde (övergripande journey)**
1. Publik: arrangör loggar in eller skapar konto.
2. Publik: arrangör skapar arrangemang, fyller i sessionsönskemål och skickar in för granskning.
3. Admin: ser att nytt arrangemang finns i granskningskön.
4. Admin: granskar, justerar innehåll och schemalägger sessioner i salar så nära önskemålen som möjligt.
5. Admin: publicerar arrangemanget.
6. Publik: arrangör ser att arrangemanget är godkänt och hur schema/sessionsplacering ser ut.
7. Publik: arrangör lämnar ändringsförslag som kommentar.
8. Admin: ser obehandlade kommentarer, genomför justeringar, markerar kommentaren som behandlad och svarar med vad som ändrats.
9. Publik: arrangör ser admins svar och kvitterar att kommentaren är hanterad.

**Avgränsning och leveransnivå**
- Detta är en epic/workflow och bryts ned i flera separata use cases i `docs/UseCases.md`.
- Frontenddokumentation beskriver aktörernas journeys (arrangör respektive admin) och förväntad upplevelse per steg.

**Statusmodell som ska vara tydlig i UI och API**
- Arrangemang: Utkast -> Inskickad -> Under granskning -> Publicerad.
- Kommentarer: Ny -> Under behandling -> Besvarad av admin -> Kvitterad av arrangör.

**Definition of Done för 3.1.6b**
- Flödet ovan går att genomföra utan manuella sidospår i båda apparna.
- Arrangör och admin ser tydlig status i varje steg.
- Kommentarer är spårbara med historik över fråga, åtgärd och kvittens.
- Relevanta notifieringar eller tydliga indikatorer finns för nya/obehandlade ärenden.
- `docs/UseCases.md` och `docs/Frontend.md` är uppdaterade enligt genomförd implementation.

**Teststrategi (nuvarande beslut)**
- Enhetstester och applikationstester per use case behålls som primär regressionssäkring.
- Backend-integrationstester ska täcka hela serverkedjan för huvudscenariot i 3.1.6b.
- Browserbaserade E2E-journeys skjuts upp och hanteras senare enligt posten i Teknisk skuld.

#### ~~3.1.7 Bemanningshantering~~ ✓ Klar
- ~~Passöversikt per station: `GET /stations/{id}/shifts`~~
- ~~Skapa pass, tilldela/bekräfta/avslå tilldelningar~~
- ~~Staffansökanslista: acceptera/avslå~~
- ~~`GET /editions/{id}/staff-applications` implementerad~~

#### ~~3.1.7b Bemanningsvy – genomgång och förfining~~ ✓ Klar
- ~~Ansökningslista med statusfilter (Att granska / Godkända / Avslagna / Alla) och räknare~~
- ~~Acceptera / avslå ansökan direkt från listan~~
- ~~Tillgänglighetsperioder visas per ansökan~~
- ~~"Lägg till funktionär" – skapar person om ny e-post, återanvänder befintlig person annars; sätter ansökan direkt som Godkänd~~

#### ~~3.1.9 Kontohantering i personregistret~~ ✓ Klar
- ~~Personlistan visar om person har kopplat konto (`hasAccount`/`isLocked` i `PersonDto`)~~
- ~~Knapp "Skicka återställningslänk" per person → `POST /persons/{id}/send-reset-link`~~
- ~~Knapp "Lås konto" / "Lås upp konto" → `UserManager.SetLockoutEnabledAsync` + `SetLockoutEndDateAsync`~~

#### ~~3.1.10 Rollvyer per upplaga~~ ✓ Klar

Löser problemet med att urvalslistor idag visar hela personregistret utan filtrering. Ingen ny domänentitet – rollerna deriveras ur befintliga register via read-only query-endpoints.

**Princip**

Rollerna är inte lagrade – de är härledda. Varje vy frågar sin källtabell direkt:

| Vy | Källa | Filter |
|----|-------|--------|
| **Besökare** | `VisitorRegistration` | Bekräftad registrering för upplagan |
| **Arrangörer** | `Event` + `CoOrganiser` | `Published`-evenemang i upplagan; både huvudarrangör och medarrangörer inkluderas |
| **Funktionärer** | `StaffApplication` | Godkänd ansökan för upplagan |
| **Ansvariga** | `Edition`, `StaffArea`, `Category` | En rad per ansvarigposition; person kan vara otillsatt (`null`) |

**Nya query-endpoints (IsAdmin)**

| Endpoint | Beskrivning |
|----------|-------------|
| `GET /editions/{id}/visitors` | Personlista – bekräftade besökarregistreringar |
| `GET /editions/{id}/organisers` | Personlista – arrangörer (lead + co) på publicerade evenemang |
| `GET /editions/{id}/staff` | Personlista – godkända funktionärsansökningar (`Assigned` eller `Confirmed`) |
| `GET /editions/{id}/responsibles` | Funktionscentrerad positionslista – en rad per definierad ansvarigfunktion |

**Ansvariga-vyn i detalj**

Funktionscentrerad vy: varje rad representerar en definierad funktion i upplagan, inte en person. Aggregerar:
- Bemanningskoordinator (`Edition.StaffCoordinatorId`) – kan vara otillsatt
- Evenemangskoordinator (`Edition.EventCoordinatorId`) – kan vara otillsatt
- En rad per funktionsområde (`StaffArea.ResponsibleId`) – alltid tillsatt (obligatoriskt i domänen)
- En rad per kategori (`Category.ResponsibleId`) – alltid tillsatt (obligatoriskt i domänen)
- En rad per publicerat evenemang – huvudarrangör (`Event.LeadOrganiserId`) – alltid tillsatt
- En rad per medarrangör på publicerade evenemang (`CoOrganiser.PersonId`) – alltid tillsatt

Koordinatorposter utan tillsatt person visas som "Ej tillsatt". Vyn är sökbar (på funktion eller personnamn).

De tre övriga vyerna (Besökare, Arrangörer, Funktionärer) är personcentrerade: de visar vilka *personer* som har en given roll. Ansvariga-vyn är funktionscentrerad: den visar vilka *funktioner* som finns och vem som innehar dem.

**Urvalslistor filtreras på funktionärer**

När admin väljer person till en ansvarigpost (koordinator, funktionsområdes- eller kategoriansvarig) hämtas urvalet från `/editions/{id}/staff` – det krävs en godkänd funktionärsansökan (`Assigned` eller `Confirmed`) för att kunna tilldelas ett ansvar.

**Edition Bootstrap**
- `staffCoordinatorId` och `eventCoordinatorId` på `Edition` är redan valfria – en ny upplaga kan skapas utan att koordinatorposterna är tillsatta

**Frontend – fyra nya flikar/vyer under upplagekontext**
- Besökare, Arrangörer, Funktionärer: personcentrerade listor med namn, e-post och relevant kontextinfo (t.ex. biljettyp, evenemangstitel, stationsval)
- Ansvariga: funktionscentrerad positionstabell med sökfunktion, read-only

*Löser teknisk skuld:* "Skalbart val av ansvariga personer"

#### 3.1.8 Registreringsöversikt
- Biljettyper: skapa, visa
- Besökarregistreringar: lista, bekräfta betalning, makulera biljett
- *Kräver ny backend-endpoint:* `GET /editions/{id}/visitor-registrations`, `GET /editions/{id}/ticket-types`

#### 3.1.11 Öppna och stänga ansökan – arrangemang och funktionärer

Admin ska kunna styra när en upplaga tar emot arrangemangsansökningar respektive funktionärsansökningar. Flödet speglar redan befintlig besökarregistrering (3.1.4), men gäller två separata processer med egna öppna/stäng-kontroller.

**Domän – `Edition`**
- Två nya booleska flaggor: `EventSubmissionsOpen` och `StaffApplicationsOpen`
- Fyra nya domänmetoder: `OpenEventSubmissions()`, `CloseEventSubmissions()`, `OpenStaffApplications()`, `CloseStaffApplications()`
- Fyra nya domänundantag: `EventSubmissionsAlreadyOpenException`, `EventSubmissionsNotOpenException`, `StaffApplicationsAlreadyOpenException`, `StaffApplicationsNotOpenException`
- `SubmitForReview`-kommandot kontrollerar att `EventSubmissionsOpen == true`; annars kastas `EventSubmissionsNotOpenException`
- `SubmitStaffApplication`-kommandot kontrollerar att `StaffApplicationsOpen == true`; annars kastas `StaffApplicationsNotOpenException`

**Nya commands**

| Command | Domänmetod | Krav |
|---------|------------|------|
| `OpenEventSubmissionsCommand` | `edition.OpenEventSubmissions()` | IsAdmin |
| `CloseEventSubmissionsCommand` | `edition.CloseEventSubmissions()` | IsAdmin |
| `OpenStaffApplicationsCommand` | `edition.OpenStaffApplications()` | IsAdmin |
| `CloseStaffApplicationsCommand` | `edition.CloseStaffApplications()` | IsAdmin |

**Nya API-endpoints**

| Endpoint | Beskrivning | Auth |
|----------|-------------|------|
| `POST /editions/{id}/event-submissions/open` | Öppnar arrangemangsansökan | IsAdmin |
| `POST /editions/{id}/event-submissions/close` | Stänger arrangemangsansökan | IsAdmin |
| `POST /editions/{id}/staff-applications/open` | Öppnar funktionärsansökan | IsAdmin |
| `POST /editions/{id}/staff-applications/close` | Stänger funktionärsansökan | IsAdmin |

**`EditionDto` utökas** med `eventSubmissionsOpen: bool` och `staffApplicationsOpen: bool` för att admin-appen och den publika appen ska kunna visa aktuell status och dölja formulär när ansökan är stängd.

**Admin-UI**
- Upplage-detaljvyn (3.1.4) utökas med två statusrader och två knappar: "Öppna arrangemangsansökan" / "Stäng arrangemangsansökan" och "Öppna funktionärsansökan" / "Stäng funktionärsansökan"
- Knapparna växlar beroende på nuvarande status (visar alltid den relevanta åtgärden)

**Publik UI**
- `SubmitForReview`-knappen i arrangörsvyn döljs eller inaktiveras om `eventSubmissionsOpen == false`
- Funktionärsansökningsformuläret (3.2.8) döljs om `staffApplicationsOpen == false`

---

### Fas 3.2 – Publik vy

Konventionsstyld app för besökare, staff och arrangörer. Deployed en gång per konvention.
Se `docs/public-mockup.html` för interaktiv skissbild av alla skärmar.

#### ~~3.2.1 Scaffold och layout~~ ✓ Klar
- ~~`ShellComponent` med `mat-toolbar` (konventionsbrandad topnav), footer~~
- ~~Angular Material custom theme via CSS custom properties (`--brand-primary`, `--brand-accent`)~~
- ~~Route-split: publika routes (`/`, `/program`, `/program/:id`, `/login`) + skyddade (`/mina-sidor/**`)~~ *(routes för `/register`, `/confirm-email`, `/forgot-password`, `/reset-password` tillkommer i 3.2.3)*
- ~~`authGuard` på alla `/mina-sidor/**`-routes – ingen `adminGuard`~~
- ~~`EditionService` (singleton): laddar aktiv upplaga vid app-start via `APP_INITIALIZER`, exponerar `editionId` som signal~~
- ~~Skeleton shimmer utility-klass i `styles.scss`~~
- ~~`EditionService.load()` anropar `GET /feed/active-edition` – admin styr publikt synlig upplaga via admin-appen~~

#### ~~3.2.2 Hem och program~~ ✓ Klar
- ~~Landningssida: hero, CTA-kort för besökare/arrangör/staff, utvalda evenemang~~
- ~~Evenemangslista (`/program`): dag-tabs (Alla/Fredag/Lördag/Söndag), kategori-filter chips, evenemangskort med border-left accent~~
- ~~Evenemangsdetalj (`/program/:id`): tvåkolumns-layout, sessionslista med expand/collapse, registreringsknapp~~
- ~~Publika endpoints: `GET /feed/editions/{id}`, `GET /feed/events/{id}`, `GET /feed/active-edition`~~
- ~~Aktiv upplaga: Convention-aggregatet lagrar `ActiveEditionId`, admin sätter via `POST /editions/{id}/set-active`~~

#### ~~3.2.3 Konton, inloggning och profil~~ ✓ Klar

**Registrering**
- Registreringsformulär (`/register`): namn, e-post och lösenord
- `POST /auth/register` med `{ name, email, password }` skapar `ApplicationUser` med `Name`, `EmailConfirmed = false` och skickar bekräftelse-e-post
- Returnerar `400` om e-postadressen redan är registrerad ("E-postadressen används redan.")
- `ApplicationUser` utökas med `Name`-property (används av UC002 vid första login)
- UC002-länkning sker vid `POST /auth/login` (som vanligt), inte vid registrering – men använder `user.Name` i stället för tomt namn vid personskapandet

**E-postbekräftelse**
- Bekräftelsesida (`/confirm-email?email=...&token=...`): anropas via länk i e-postmeddelande
- `POST /auth/confirm-email` med `{ email, token }` → `UserManager.ConfirmEmailAsync`
- Länk till "Skicka om bekräftelse" om token gått ut
- `POST /auth/resend-confirmation` med `{ email }` – returnerar alltid 200 (avslöjar inte om kontot finns)
- Login returnerar `403` med tydlig instruktion om e-posten inte är bekräftad

**Glömt lösenord (self-service)**
- Glömt lösenord-sida (`/forgot-password`): anger e-postadress
- `POST /auth/forgot-password` – returnerar alltid 200; skickar länk om konto finns och e-post är bekräftad
- Återställningssida (`/reset-password?email=...&token=...`): anger nytt lösenord
- `POST /auth/reset-password` med `{ email, token, newPassword }` → `UserManager.ResetPasswordAsync`
- Tokens URL-encodas i länken (innehåller specialtecken), decodas på backend

**Lösenordsbyte (inloggad)**
- Profilsida (`/mina-sidor/profil`): visar namn/e-post/telefon, formulär för lösenordsbyte
- `PUT /auth/password` med `{ currentPassword, newPassword }` → `UserManager.ChangePasswordAsync`
- `GET /me/profile`, `PUT /me/profile` för profilfälten

*Kräver nya backend-endpoints:* `POST /auth/register`, `POST /auth/confirm-email`, `POST /auth/resend-confirmation`, `POST /auth/forgot-password`, `POST /auth/reset-password`, `PUT /auth/password`, `GET /me/profile` – (`PUT /me/profile` redan implementerad)

*Kräver modellförändring:* `ApplicationUser` utökas med `string Name`-property; `POST /auth/login` uppdateras att läsa `user.Name` vid personskapandet i UC002, samt att returnera `403` om `EmailConfirmed == false`.

*Kräver e-posttjänst:* `LoggingEmailService` i dev (loggar länkarna i konsolen utan SMTP). Tre nya metoder i `IEmailService`: `SendEmailConfirmationAsync`, `SendResendConfirmationAsync`, `SendPasswordChangedAsync` (`SendPasswordResetAsync` finns redan). Fyra e-posttyper totalt: välkommen+bekräftelse, skicka om bekräftelse, lösenordsåterställning, lösenord ändrat.

#### 3.2.4 Mina sidor – hub och navigationsstruktur
- `MinaSidorComponent` (hub): hälsningsbanner + kompakta statuskort per sektion
- Navigationsstruktur (alltid synlig, alla sektioner, CTA om tomt):
  - **Min biljett** – rollneutral (alla som deltar behöver en biljett)
  - *Som besökare:* **Mitt program** – sessioner man anmält sig till
  - *Som arrangör:* **Mina arrangemang** – lista + skapa/redigera
  - *Som funktionär:* **Min bemanning** – ansökan + tilldelade pass
- Laddar alla fyra datasektioner parallellt i `ngOnInit`

#### 3.2.5 Min biljett
- Visar biljetttyp, referensnummer och betalningsstatus om registrerad
- Tomt state: biljettval via radio-cards (Helg-biljett, Dagsbiljetter), kontaktuppgifter
  förifyllda från profil, villkorscheckbox, info om separat betalning
- `POST /editions/{id}/visitor-registrations` vid submit
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-visitor-registration`

#### 3.2.6 Mitt program (som besökare)
- Lista sessioner man anmält sig till: evenemang, tid, lokal, platsnummer
- Avbokning direkt från listan
- Tomt state: uppmaning att bläddra i `/program`
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-session-registrations`

#### 3.2.7 Mina arrangemang (som arrangör)
- Lista med alla egna arrangemang: titel, kategori, antal sessioner, status
- "Nytt arrangemang"-knapp i list-header
- Tomt state: formulär direkt inbäddat (eller länk till `/mina-sidor/arrangemang/nytt`)
- Formulär: titel, kategori, beskrivning, registreringstyp, sessionönskemål
- Skicka in för granskning (`POST /events/{id}/submit`), dra tillbaka till utkast
- Detaljvy visar adminkommentar (gul alert) och status-chip
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-events`

#### 3.2.8 Min bemanning (som funktionär)
- Ansökningsformulär: fritextmotivering, stationspreferenser, tillgänglighet (Fre/Lör/Sön)
- `POST /editions/{id}/staff-applications` vid submit
- Statusvy om ansökan redan finns: chip-status + tilldelade pass-lista
- *Kräver ny backend-endpoint:* `GET /editions/{id}/my-staff-application`

#### 3.2.9 Sessionsregistrering
- Anmäl till enskild session direkt från evenemangsdetalj-sidan
- Kapacitetsindikator (grön/orange/röd beroende på fyllnadsgrad)
- `POST /sessions/{id}/registrations` vid anmälan
- Avboka: `DELETE /session-registrations/{id}`
- Anmälda sessioner syns under "Mitt program"

#### 3.2.11 Personligt tidsschema – samlad vy i Mitt program

En vy som visar *alla* egna engagemang under konventet i kronologisk ordning – oavsett vilken roll man har. Tanken är att besökaren ska kunna öppna ett enda ställe och se hela sin helg utan att behöva navigera mellan sektionerna.

**Händelsetyper som ingår**

| Typ | Källa | Visas som |
|-----|-------|-----------|
| Bokad session (besökare) | `SessionRegistration` | Primär – platsbiljett |
| Bevakad session | `SessionWatch` | Sekundär – "Vill se" |
| Session på eget arrangemang | `Event.Sessions` där personen är lead- eller medarrangör | Sekundär – "Arrangör" |
| Tilldelat bemanningspass | `ShiftAssignment` (Confirmed/Assigned) | Primär – "Pass" |

En session kan förekomma i flera kategorier (t.ex. arrangör *och* bevakning) – visas som en rad med kombinerade etiketter.

**Kollisionsindikatorer**
- Om två primära händelser överlappar i tid markeras de med en varningsindikator
- Bevakning och arrangörsroll räknas inte som primärt block och utlöser inga varningar mot varandra

**Backend – ny samlad query**
- `GET /editions/{id}/my-schedule` returnerar alla händelser sorterade på starttid
- Varje rad innehåller: `sessionId`, `eventTitle`, `start`, `end`, `venueName`, `type` (`booked | watched | organiser | shift`), `shiftId?`
- Händelsetypen `shift` hämtar data från Staff-kontexten (cross-context read, samma mönster som `my-session-registrations`)

**Frontend – tidslinjevy i "Mitt program"**
- Ny flik eller vy under `/mina-sidor/program`: "Tidslinje" (komplement till befintlig sessionslista)
- Grupperat per dag (Fredag / Lördag / Söndag)
- Varje händelse visas som ett kort med tidsintervall, typ-chip (Bokat / Arrangör / Vill se / Pass) och lokal
- Kolliderande primärhändelser markeras med orange bakgrund eller varningsikon

Besökare ska kunna markera sessioner de är intresserade av utan att boka en plats. Markeringen syns i "Mitt program" som en separat sektion ("Vill se") och ger en personlig programöversikt även för sessioner med fri entré eller utan platskapacitet.

**Avgränsning**
- En bevakning är *inte* en platsbiljett – den reserverar ingen plats och påverkar inte kapacitetsräknare
- Bevakning och bokning är oberoende: man kan bevaka en session man redan bokat, och avboka en bokning utan att ta bort bevakningen
- Bevakning kräver inloggning men *inte* besökarregistrering (biljett)

**Domän – ny entitet `SessionWatch`**
- Tillhör Registration-kontexten
- Fält: `PersonId`, `SessionId`, `EditionId`, `CreatedAt`
- Unikt per `(PersonId, SessionId)` – inga dubletter
- Inga statusövergångar: antingen bevakad eller inte

**Nya API-endpoints**

| Endpoint | Beskrivning | Auth |
|----------|-------------|------|
| `POST /sessions/{id}/watch` | Lägg till bevakning | Autentiserad |
| `DELETE /sessions/{id}/watch` | Ta bort bevakning | Autentiserad |
| `GET /editions/{id}/my-watched-sessions` | Lista bevakade sessioner | Autentiserad |

**Frontend**
- Evenemangsdetalj-sidan (publik, `/program/:id`) visar ett bokmärkesikons-knappar per session: fyllt = bevakad, tomt = ej bevakad
- "Mitt program"-sektionen (3.2.6) delas i två: **Bokade** (platsbiljett) och **Vill se** (bevakning)
- Bevakning kan läggas till/tas bort direkt från "Mitt program"-listan

---

### Backend-komplement som krävs under Fas 3

Byggs precis innan den frontendsektion som behöver dem.

#### Auth och konton (3.2.3 + 3.1.9)

| Endpoint | Syfte | Auth |
|----------|-------|------|
| `POST /auth/register` | Skapar konto, skickar bekräftelse-e-post | Anonym |
| `POST /auth/confirm-email` | Bekräftar token från e-postlänk | Anonym |
| `POST /auth/resend-confirmation` | Skickar ny bekräftelselänk | Anonym |
| `POST /auth/forgot-password` | Genererar reset-token, skickar e-post | Anonym |
| `POST /auth/reset-password` | Sätter nytt lösenord med token | Anonym |
| `PUT /auth/password` | Byter eget lösenord (inloggad) | Autentiserad |
| `GET /me/profile` | Hämtar inloggad persons profil | Autentiserad |
| `PUT /me/profile` | Uppdaterar profil (namn, e-post, telefon) | Autentiserad |
| `POST /persons/{id}/send-reset-link` | Admin skickar reset-e-post åt person | IsAdmin |

**Viktiga detaljer:**
- `POST /auth/login` utökas: kontrollerar `EmailConfirmed` → `403` med instruktion om ej bekräftad
- Tokens (bekräftelse + reset) URL-encodas i e-postlänkar, decodas på backend
- `PersonDto` utökas med `hasAccount: bool` (join mot `identity.users` på `person_id`)
- `DevDataSeeder` och `ConventionSystemFactory` sätter `EmailConfirmed = true` direkt – kringgår e-postflödet

#### Rollvyer (3.1.10)

Inga nya domänentiteter. Fyra read-only query-endpoints som deriverar rollerna ur befintliga register:

| Endpoint | Syfte | Auth |
|----------|-------|------|
| `GET /editions/{id}/visitors` | Bekräftade besökarregistreringar | IsAdmin |
| `GET /editions/{id}/organisers` | Arrangörer (lead + co) på publicerade evenemang | IsAdmin |
| `GET /editions/{id}/staff` | Godkända funktionärsansökningar | IsAdmin |
| `GET /editions/{id}/responsibles` | Positionslista – en rad per ansvarigpost, person kan vara null | IsAdmin |

Urvalslistor för koordinator- och ansvarigval hämtar från `/editions/{id}/staff`.

#### Övriga endpoints (övriga 3.x-sektioner)

| Endpoint | Krävs för | Auth |
|----------|-----------|------|
| `GET /editions/{id}/persons` | 3.1.5 personregister | IsAdmin |
| ~~`GET /editions/{id}/staff-applications`~~ | ~~3.1.7 bemanningshantering~~ ✓ Klar | IsAdmin |
| ~~`GET /feed/active-edition`~~ | ~~3.2.1–3.2.2 publik vy~~ ✓ Klar | Anonym |
| ~~`POST /editions/{id}/set-active`~~ | ~~3.2.2 admin sätter aktiv upplaga~~ ✓ Klar | IsAdmin |
| `GET /editions/{id}/visitor-registrations` | 3.1.8 registreringsöversikt | IsAdmin |
| `GET /editions/{id}/ticket-types` | 3.1.8 biljettyper | Publik |
| `GET /editions/{id}/my-visitor-registration` | 3.2.5 besökarregistrering | Autentiserad |
| `GET /editions/{id}/my-session-registrations` | 3.2.6 mitt program | Autentiserad |
| `GET /editions/{id}/my-events` | 3.2.7 arrangörsflöde | Autentiserad |
| `GET /editions/{id}/my-staff-application` | 3.2.8 staffansökan | Autentiserad |
| `GET /editions/{id}/my-watched-sessions` | 3.2.10 bevakningslista | Autentiserad |
| `POST /sessions/{id}/watch` | 3.2.10 lägg till bevakning | Autentiserad |
| `DELETE /sessions/{id}/watch` | 3.2.10 ta bort bevakning | Autentiserad |
| `GET /editions/{id}/my-schedule` | 3.2.11 samlat personligt tidsschema | Autentiserad |
| `POST /editions/{id}/event-submissions/open` | 3.1.11 öppna arrangemangsansökan | IsAdmin |
| `POST /editions/{id}/event-submissions/close` | 3.1.11 stänga arrangemangsansökan | IsAdmin |
| `POST /editions/{id}/staff-applications/open` | 3.1.11 öppna funktionärsansökan | IsAdmin |
| `POST /editions/{id}/staff-applications/close` | 3.1.11 stänga funktionärsansökan | IsAdmin |

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| ~~**Skalbart val av ansvariga personer**~~ | ~~Utvärderat och planerat: löses i 3.1.10 via `Ansvarig`-rollen och rollfiltrerad urvalslista.~~ | ~~Medel~~ – *åtgärdas i 3.1.10* |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| **Feed-cachning och API-nyckel** | Feed-endpointsen är öppna och läser från databasen vid varje anrop. Vid hög trafik (t.ex. om ett CMS pollar ofta) bör svaren cachas (HTTP-headers `Cache-Control`/`ETag`, CDN-lager eller Redis). Vid behov av skyddade feeds kan en API-nyckel i header eller query-parameter läggas till utan att ändra URL-strukturen. | Medel – utvärdera inför produktion |
| **E2E-test för journeys (admin + publik)** | Journey-flöden testas i nuläget via enhets- och integrationstester, men saknar UI-verifiering över hela kedjan. Lägg till browserbaserade E2E-scenarier för kritiska end-to-end-flöden (t.ex. 3.1.6b) när funktionerna stabiliserats. | Medel – planera efter implementation av 3.x-flöden |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person (admin-väg och auth-väg). Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande | Medel – se till att UC002-vägen aldrig kolliderar |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person simultaneously | Låg – unikt index är sista skyddet |
| `ICurrentUser` i bakgrundsjobb | `ICurrentUser` läser från `HttpContext` och fungerar inte utanför HTTP-request-scopet. Bakgrundsjobb och seeders måste anropa domänmodellen direkt och förbigå handlers som kräver `ICurrentUser`. | Medel – dokumentera mönstret |

---

## Fas 4 – Demo och driftsättning

### 4.1 Demo-deploy (ett fiktivt konvent)
- Bygg-pipeline: Angular-appar (admin + publik) byggs in i `wwwroot` som en del av .NET publish-steget
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

---

## Enkel lista – Tänkt implementationsordning

Använd en rad per punkt och ändra bara statusmarkören i början.

- [ ] `R02` Fas 3.1.8 Registreringsöversikt i admin
- [ ] `R12` Fas 3.1.11 Öppna och stänga ansökan – arrangemang och funktionärer
- [ ] `R04` Fas 3.2.4 Mina sidor – hub och navigationsstruktur
- [ ] `R05` Fas 3.2.5 Min biljett
- [ ] `R06` Fas 3.2.6 Mitt program
- [ ] `R09` Fas 3.2.9 Sessionsregistrering
- [ ] `R13` Fas 3.2.10 Bevakningslista – sessioner utan platsbiljett
- [ ] `R14` Fas 3.2.11 Personligt tidsschema – samlad vy i Mitt program
- [ ] `R08` Fas 3.2.8 Min bemanning
- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent
- [x] `R00` Frontendtester i CI
- [x] `R01` Fas 3.2.3 Konton, inloggning och profil
- [x] `R10` Fas 3.1.7b Bemanningsvy – genomgång och förfining
- [x] `R07` Fas 3.2.7 Mina arrangemang
- [x] `R03` Fas 3.1.6b Evenemangsflöde – genomgång och förfining

### Snabbregler för uppdatering
- Behåll `Rxx`-id så att referenser i commits och PR-beskrivningar blir stabila.
- Status: `- [ ]` = ej startad, `- [~]` = pågår, `- [x]` = klar.
- Sortera listan uppifrån och ned efter prioritet när ordningen ändras.

---
## UX Justeringar
Uppdatera även frontenddokumentationen där dessa fixar görs.

### UX001 Datum och tid i formulär
**I administrationsgränssnittet**
* Gränssnittet skall hjälpa till med att:
  * om slutdatum inte är satt: utifrån en input parameter till controllen sätta slutdatum/tid med den offseten. ex 1h.
  * om sluttiden är satt och man justerar starttiden, så skall sluttiden justeras med motsvarande
  * göra det emklare att endast väla de datum som är mellan start och slut på konventet.
  
### UX002 Datum och tid i listor
* Sortera tabeller som innehåller start och sluttid efter starttid i fallande ordning som standard
  

