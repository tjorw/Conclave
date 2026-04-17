# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – ej startade överst, klara underst.

- [ ] `R15` Biljettmodell reviderad – `validDays`, `allowedCategories`, `TicketPerk` (UC-TK001/TK002)
- [ ] `R16` Biljettlivscykel reviderad – manuell betalning, webhook, innehavaravbokning (UC-TK003–TK006)
- [ ] `R17` Makuleringskaskad + uthämtning med förmåner (UC-TK007/TK008)
- [ ] `R18` `RegistrationRuleService.ValidateTicket` med dag- och kategorivalidering (UC-TK009)
- [x] `R14` Fas 3.2.11 Personligt tidsschema – samlad vy i Mitt program
- [x] `R08` Fas 3.2.8 Min bemanning
- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent
- [x] `R13` Fas 3.2.10 Bevakningslista – sessioner utan platsbiljett
- [x] `R05` Fas 3.2.5 Min biljett

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

---

## Fas 3 – Återstående frontend

### Admin-app


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
| **PersonId från klient i self-service registration** | Registration-endpoints tar `PersonId` i request-body (`SubmitVisitorRegistrationRequest`, `RegisterForSessionRequest`) och skickar vidare till handlers. Detta kopplar transportmodell till säkerhetsmodell och avviker från mönstret med server-side `ICurrentUser`. **Buggrisk:** användare kan försöka agera för annan person via manipulerad payload. | **Hög – byt till `ICurrentUser` i API/Application för self-service-flöden** |
| **Fel exception-typ i Registration Application** | Registration-handlers kastar brett `InvalidOperationException` i stället för semantiska typer enligt `Backend.md` (`ResourceNotFoundException`, `ForbiddenException`, `DomainRuleViolationException`). Detta försvagar API-kontrakt, error mapping och observability. | **Hög – standardisera exceptions enligt riktlinje** |
| **`Shift` saknar `EditionId`** | `Shift` har ingen direkt koppling till `EditionId`. `MyScheduleRepository` löser detta via `Edition.Stations`-navigeringen (shadow FK). Om Shift-kontexten växer bör ett direkt `EditionId` övervägas på `Shift` för att slippa join-beroendet mot Convention. | Låg – fungerar korrekt, men fragil vid schemamigration |
| **Deduplikering i tidsschema** | Om samma session förekommer i flera kategorier (t.ex. bokad OCH arrangör) prioriteras Booked > Organiser > Watching i `MyScheduleRepository`. Prioriteringslogiken är inte testad på domännivå. Om affärsreglerna ändras (t.ex. "visa alltid arrangörsrollen oavsett bokning") behöver deduplikeringen ses över. | Låg – nuvarande beteende är rimligt |
| **Inga `DbSet<Station>` i `ConventionDbContext`** | `Station` och `Venue` nås via `db.Set<T>()` i stället för namngivna `DbSet<T>`-properties. Inkonsekvens mot övriga entiteter. Lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` om fler queries börjar hämta dem direkt. | Låg |
| Hårdkodade strängar i repository | Det finns hårdkodade texter i repot för att göra urval på. t.ex. "booked". Stor risk för buggar om det fortsätter att vara magic strings. | Medel |
| **Gamla TK-implementationer behöver revideras** | UC-TK001–TK004 (gamla) är implementerade men matchar inte längre UC-specen: `TicketType` saknar `validDays`/`allowedCategories`/`TicketPerk`; betalningsflödet är inbyggt i VR-flödet i stället för separat; makulering saknar kaskad mot `SessionRegistrations`. Dessa måste reskrivas som en del av R15–R18. | **Hög – blockar korrekt sessionsvalidering** |

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