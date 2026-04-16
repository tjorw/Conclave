# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – ej startade överst, klara underst.

- [ ] `R15` Fas 3.1.12 Globalt schemaläggningsverktyg (admin)
- [ ] `R16` Fas 3.1.13 Sessionsredigerare i evenemangsdetalj (admin)
- [ ] `R05` Fas 3.2.5 Min biljett
- [ ] `R06` Fas 3.2.6 Mitt program
- [ ] `R09` Fas 3.2.9 Sessionsregistrering
- [ ] `R13` Fas 3.2.10 Bevakningslista – sessioner utan platsbiljett
- [ ] `R14` Fas 3.2.11 Personligt tidsschema – samlad vy i Mitt program
- [ ] `R08` Fas 3.2.8 Min bemanning
- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent
- [x] `R00` Frontendtester i CI
- [x] `R01` Fas 3.2.3 Konton, inloggning och profil
- [x] `R02` Fas 3.1.8 Registreringsöversikt i admin
- [x] `R03` Fas 3.1.6b Evenemangsflöde – genomgång och förfining
- [x] `R04` Fas 3.2.4 Mina sidor – hub och navigationsstruktur
- [x] `R07` Fas 3.2.7 Mina arrangemang
- [x] `R10` Fas 3.1.7b Bemanningsvy – genomgång och förfining
- [x] `R12` Fas 3.1.11 Öppna och stänga ansökan – arrangemang och funktionärer

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

---

## Fas 3 – Återstående frontend

### Admin-app

#### 3.1.12 Globalt schemaläggningsverktyg (admin)

Ny menypost "Schemaläggning" i admin-sidnavet. Ger konventionsadmin en samlad vy över alla sessioner och möjlighet att placera, flytta och verifiera schemaläggning utan att behöva navigera in i varje enskilt evenemang.

**Skärmlayout**

Tvådelad vy: vänster sidopanel med åtgärdsformulär och höger tidslinjepanel.

*Sidopanel*
- **Förslagsformulär:** välj evenemang → välj sessionönskemål (dropdown med önskemålets tid/platser), välj lokal, sätt start- och sluttid, konfliktindikator (röd varning direkt om överlapp detekteras), valfri intern kommentar
- **"Ta in önskemål"**-knapp på varje rad i önskemålslistan: fyller i formuläret med önskemålets parametrar som startpunkt
- **Sessionönskemålslista:** visar alla oschemalagda önskemål (evenemangstitel, önskad tid, platser, typ), sorterade per evenemang

*Tidslinjepanel*
- Dag-flikar (en per konventdag) eller datumväljare
- Lokaler som rader, tid (08:00–22:00) som x-axel
- Sessionblock med färgkodning:
  - Grön – placerad session
  - Orange – pågående redigering / förslag
  - Röd – konflikt (överlapp i samma lokal och tid)
- Filter: byggnad (grupperar lokaler), kategori, fritextsökning

**Konfliktdetektering**
- Sker i frontend mot lokalt cachad sessionlista
- En session är i konflikt om start/slut överlappar med en annan session i samma lokal
- Konflikten visas i formuläret (röd statusrad) och i tidslinjen (röd block)

**Åtgärder**
- Spara nytt sessionblock → `POST /events/{eventId}/sessions`
- Uppdatera befintlig session → `PUT /events/{eventId}/sessions/{sessionId}`
- Inaktivera session → `DELETE /events/{eventId}/sessions/{sessionId}`

**Backend – ny query-endpoint**

| Endpoint | Beskrivning | Auth |
|----------|-------------|------|
| `GET /editions/{id}/sessions` | Alla sessioner för upplagan: `sessionId`, `eventId`, `eventTitle`, `venueId`, `start`, `end`, `maxSeats`, `startType`, `status` | IsAdmin |

**Frontend**
- Ny lazy-loadad route: `/sessions` i admin-appen
- `SessionsOverviewComponent`: laddar `GET /editions/{id}/sessions` + `GET /editions/{id}` (lokaler, kategorier) i parallell
- `TimelineComponent`: gemensam komponent som också används i 3.1.13 nedan

---

#### 3.1.13 Sessionsredigerare i evenemangsdetalj (admin)

Kompletterar den befintliga formulärbaserade sessionsvyn med en valfri tidslinjevy. Enkla formulärkontroller behålls för snabb datainmatning; tidslinjevyn är ett alternativ att växla till när man vill se lokalens kontext.

**Princip: två lägen, inte ett ersatt**

Sessionsfliksn erbjuder en växlingsknapp "Visa tidslinje" / "Dölj tidslinje":
- **Standardläge (formulär):** befintliga kontroller – lokal, starttid, sluttid, platser, starttyp, Spara/Återställ/Inaktivera.
- **Tidslinjläge (komplement):** tidslinjen visas bredvid formuläret och uppdateras reaktivt när lokal eller tider ändras i formuläret.

**Tidslinjevy**
- Tid 08:00–23:00, vertikal layout med vald lokal som kontext
- Sessionblock med färgkodning:
  - Grön – eget evenemangs sessioner
  - Grå – andra evenemang i samma lokal (read-only)
  - Orange – session under aktiv redigering / ny session
  - Röd – konflikt
- Tidslinjen uppdateras när användaren byter lokal eller tid i formuläret

**Datakälla**
- Egna sessioner: redan hämtade via `GET /events/{id}` (ingår i `EventDto`)
- Andra evenemang i lokal: hämtas från `GET /editions/{id}/sessions` (se 3.1.12), filtreras i frontend på vald lokal – laddas lazy när tidslinjeläget aktiveras för första gången

**Gemensam komponent: `TimelineComponent`**

| Input | Typ | Beskrivning |
|-------|-----|-------------|
| `sessions` | `SessionBlock[]` | Alla sessioner att rendera |
| `highlightEventId` | `string \| null` | Eget evenemang (grön färg) |
| `draftBlock` | `DraftBlock \| null` | Pågående redigering (orange) |
| `venues` | `VenueDto[]` | Lokal-metadata |
| `selectedVenueId` | `string \| null` | Filtrerar tidslinjen till en lokal |

Konflikter beräknas internt i komponenten baserat på `draftBlock` mot `sessions`.

---

### Publik vy

#### 3.2.5 Min biljett
- Visar biljetttyp, referensnummer och betalningsstatus om registrerad
- Tomt state: biljettval via radio-cards, kontaktuppgifter förifyllda från profil, villkorscheckbox, info om separat betalning
- `POST /editions/{id}/visitor-registrations` vid submit
- *Kräver backend:* `GET /editions/{id}/my-visitor-registration`

#### 3.2.6 Mitt program (som besökare)
- Lista sessioner man anmält sig till: evenemang, tid, lokal, platsnummer
- Avbokning direkt från listan
- Tomt state: uppmaning att bläddra i `/program`
- *Kräver backend:* `GET /editions/{id}/my-session-registrations`

#### 3.2.8 Min bemanning (som funktionär)
- Ansökningsformulär: fritextmotivering, stationspreferenser, tillgänglighet (Fre/Lör/Sön)
- `POST /editions/{id}/staff-applications` vid submit
- Statusvy om ansökan redan finns: chip-status + tilldelade pass-lista
- *Kräver backend:* `GET /editions/{id}/my-staff-application`

#### 3.2.9 Sessionsregistrering
- Anmäl till enskild session direkt från evenemangsdetalj-sidan
- Kapacitetsindikator (grön/orange/röd beroende på fyllnadsgrad)
- `POST /sessions/{id}/registrations` vid anmälan
- Avboka: `DELETE /session-registrations/{id}`
- Anmälda sessioner syns under "Mitt program"

#### 3.2.10 Bevakningslista + 3.2.11 Personligt tidsschema

*Täcker R13 (Bevakningslista) och R14 (Personligt tidsschema) – implementeras i ett sammanhängande arbetspass.*

**Bevakningsfunktionen (R13)**

Besökare ska kunna markera sessioner de är intresserade av utan att boka en plats. Bevakning kräver inloggning men inte besökarregistrering (biljett).

- En bevakning är *inte* en platsbiljett – den reserverar ingen plats och påverkar inte kapacitetsräknare
- Bevakning och bokning är oberoende

*Domän – ny entitet `SessionWatch`*
- Tillhör Registration-kontexten
- Fält: `PersonId`, `SessionId`, `EditionId`, `CreatedAt`
- Unikt per `(PersonId, SessionId)` – inga dubletter

*Nya API-endpoints*

| Endpoint | Beskrivning | Auth |
|----------|-------------|------|
| `POST /sessions/{id}/watch` | Lägg till bevakning | Autentiserad |
| `DELETE /sessions/{id}/watch` | Ta bort bevakning | Autentiserad |
| `GET /editions/{id}/my-watched-sessions` | Lista bevakade sessioner | Autentiserad |

*Frontend*
- Evenemangsdetalj-sidan visar bokmärkesikon per session: fyllt = bevakad, tomt = ej bevakad
- "Mitt program"-sektionen (3.2.6) delas i två: **Bokade** (platsbiljett) och **Vill se** (bevakning)

**Personligt tidsschema (R14)**

En vy under `/mina-sidor/program` som visar *alla* egna engagemang under konventet i kronologisk ordning – oavsett roll.

| Typ | Källa | Visas som |
|-----|-------|-----------|
| Bokad session (besökare) | `SessionRegistration` | Primär – platsbiljett |
| Bevakad session | `SessionWatch` | Sekundär – "Vill se" |
| Session på eget arrangemang | `Event.Sessions` (lead- eller medarrangör) | Sekundär – "Arrangör" |
| Tilldelat bemanningspass | `ShiftAssignment` (Confirmed/Assigned) | Primär – "Pass" |

Kolliderande primära händelser markeras med varningsindikator. Bevakning och arrangörsroll räknas inte som primärt block.

*Backend – ny samlad query*

| Endpoint | Beskrivning | Auth |
|----------|-------------|------|
| `GET /editions/{id}/my-schedule` | Alla händelser sorterade på starttid: `sessionId`, `eventTitle`, `start`, `end`, `venueName`, `type`, `shiftId?` | Autentiserad |

*Frontend*
- Ny flik eller vy under `/mina-sidor/program`: "Tidslinje"
- Grupperat per dag (Fredag / Lördag / Söndag)
- Kolliderande primärhändelser markeras med orange bakgrund eller varningsikon

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| **Feed-cachning och API-nyckel** | Feed-endpointsen är öppna och läser från databasen vid varje anrop. Vid hög trafik bör svaren cachas (HTTP-headers `Cache-Control`/`ETag`, CDN-lager eller Redis). Vid behov av skyddade feeds kan en API-nyckel läggas till utan att ändra URL-strukturen. | Medel – utvärdera inför produktion |
| **E2E-test för journeys** | Journey-flöden saknar UI-verifiering över hela kedjan. Lägg till browserbaserade E2E-scenarier för kritiska flöden när funktionerna stabiliserats. | Medel – planera efter implementation av 3.x-flöden |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person. Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande. | Medel – UC002-vägen får aldrig kollidera |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person simultaneously. Unikt index är sista skyddet. | Låg |
| `ICurrentUser` i bakgrundsjobb | `ICurrentUser` läser från `HttpContext` och fungerar inte utanför HTTP-request-scopet. Bakgrundsjobb och seeders måste anropa domänmodellen direkt. | Medel – dokumentera mönstret |

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

## UX Justeringar
Uppdatera även frontenddokumentationen där dessa fixar görs.

### UX001 Datum och tid i formulär
**I administrationsgränssnittet**
* Gränssnittet skall hjälpa till med att:
  * om slutdatum inte är satt: utifrån en input parameter till controllen sätta slutdatum/tid med den offseten. ex 1h.
  * om sluttiden är satt och man justerar starttiden, så skall sluttiden justeras med motsvarande
  * göra det enklare att endast välja de datum som är mellan start och slut på konventet.

### UX002 Datum och tid i listor
* Sortera tabeller som innehåller start och sluttid efter starttid i fallande ordning som standard
