# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

---


### Bokning och tilldelning av plats
Platser i arrangemang kan tilldelas på olika sätt. Kön hör till det konkreta objektet man anmäler sig till; i nuvarande modell är det en session. Arrangemanget äger reglerna för hur sessionernas bokningar hanteras, till exempel om första bokningsförsöket skall bekräftas direkt eller hamna i kö/väntlista, och om samma person får boka flera sessioner i samma arrangemang.

**ADR:** `docs/decisions/2026-05-09-booking-allocation.md`

Implementationsordning: R-BK01a → R-BK01b → R-BK01c → R-BK02a → R-BK02b → R-BK02c

- [x] `R-BK01a` Domän: `AllocationMode`-enum + `Event.ConfigureAllocationMode()`; `SessionRegistrationStatus.Pending`; `SessionRegistration.Confirm()` + utökad `Cancel()`; domain events `SessionRegistrationQueued` och `SessionRegistrationConfirmed`; enhetstest
- [x] `R-BK01b` Applikation: `RegisterForSessionHandler` grenar på `AllocationMode` — `DirectConfirmation` räknar kapacitet, `Queue` skapar `Pending`; `IRegistrationRuleService.ValidateSeatAvailability` tas bort; ny repo-metod `CountConfirmedBySessionIdAsync`; `ConfigureAllocationModeCommand` + handler
- [x] `R-BK01c` Infrastruktur + API: EF-kolumn `allocation_mode` på `events`; migration `AddAllocationMode`; endpoint `PUT /api/events/{id}/allocation-mode`
- [x] `R-BK02a` Domän + Applikation: `AllocationStrategy`-enum; `AllocateSessionRegistrationsCommand` + handler med FCFS, Lottery och Manual; `ISessionRegistrationRepository.GetPendingBySessionAsync` + `SaveAllAsync`; enhetstest för alla tre strategier
- [x] `R-BK02b` Infrastruktur + API: repository-implementationer; endpoint `POST /api/events/{eventId}/sessions/{sessionId}/allocate`; behörighetscheck admin
- [ ] `R-BK01c-ui` Admin-UI: dropdown för `AllocationMode` i evenemangsdetalj
- [ ] `R-BK02c` Admin-UI: sektion i sessionslistan som visar antal `Pending` per session; knapp "Kör tilldelning" öppnar dialog med strategival och bekräftelse


### Multitenancy

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [ ] `R-MT014` `portal`-app: self-service signup (publik del)
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*


### Flerspråksstöd (R-I18N)

Stöd för att redigera och visa innehåll på flera språk. Implementeras i fas efter CMS och Varumärke. Se UC-I18N001–UC-I18N004 i `docs/UseCases.md`.

Implementationsordning: R-I18N01 → R-I18N02 → R-I18N03 → R-I18N04 → R-I18N05

- [ ] `R-I18N01` Språkstyrning – samla kvarvarande hårdkodade UI-texter bakom labels/översättningslager och förbered engelsk version
- [ ] `R-I18N02` `EditionLocale`-entitet (EditionId, Locale, IsPrimary) – admin aktiverar språk per upplaga; primärspråk alltid exakt ett (UC-I18N001)
- [ ] `R-I18N03` `PageTranslation`-entitet (PageId, Locale, Title, Content) – admin redigerar översättningar via flikbaserat UI; `GetPageBySlugQuery` utökas med locale-parameter och fallback (UC-I18N002)
- [ ] `R-I18N04` `EventTranslation`-entitet (EventId, Locale, Title, Description) – arrangör/admin redigerar översättningar; publika eventqueries tar emot locale-parameter (UC-I18N003)
- [ ] `R-I18N05` Publik språkväljare-komponent + locale-signal-service; locale skickas som query-parameter; `localStorage`-persistens; `Accept-Language`-fallback (UC-I18N004)

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| **Cache stampede i `CachingTenantResolver`** | Mönstret `TryGetValue → miss → DB → Set` utan per-nyckel-samordning kan ge N parallella DB-träffar vid burst mot samma tenant efter TTL/invalidering. Okända tenants cachas inte alls, så upprepade requests mot okänd subdomain fortsätter slå DB. Lös med per-key `SemaphoreSlim`/single-flight och överväg kort negativ cache för okända subdomains. Låg risk vid nuvarande skala. | Låg |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| **Feed-cachning och API-nyckel** | Feed-endpointsen är öppna och läser från databasen vid varje anrop. Vid hög trafik bör svaren cachas (HTTP-headers `Cache-Control`/`ETag`, CDN-lager eller Redis). Vid behov av skyddade feeds kan en API-nyckel läggas till utan att ändra URL-strukturen. | Medel – utvärdera inför produktion |
| **E2E-test för journeys** | Journey-flöden saknar UI-verifiering över hela kedjan. Lägg till browserbaserade E2E-scenarier för kritiska flöden när funktionerna stabiliserats. | Medel – planera efter implementation av 3.x-flöden |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person. Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande. | Medel – UC002-vägen får aldrig kollidera |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person simultaneously. Unikt index är sista skyddet. | Låg |
| **`Shift` saknar `EditionId`** | `Shift` har ingen direkt koppling till `EditionId`. `MyScheduleRepository` löser detta via `Edition.Stations`-navigeringen (shadow FK). Om Shift-kontexten växer bör ett direkt `EditionId` övervägas på `Shift` för att slippa join-beroendet mot Convention. | Låg – fungerar korrekt, men fragil vid schemamigration |
| **Deduplikering i tidsschema** | Om samma session förekommer i flera kategorier (t.ex. bokad OCH arrangör) prioriteras Booked > Organiser > Watching i `MyScheduleRepository`. Prioriteringslogiken är inte testad på domännivå. Om affärsreglerna ändras (t.ex. "visa alltid arrangörsrollen oavsett bokning") behöver deduplikeringen ses över. | Låg – nuvarande beteende är rimligt |
| **Inga `DbSet<Station>` i `ConventionDbContext`** | `Station` och `Venue` nås via `db.Set<T>()` i stället för namngivna `DbSet<T>`-properties. Inkonsekvens mot övriga entiteter. Lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` om fler queries börjar hämta dem direkt. | Låg |

---

## Övrig backlog
- **Debounced sökning** – extrahera gemensam RxJS-pipeline till `createSearchStream()`-helper.
- **Multi-request pending-räknare** – ersätt manuell `pending`-räknare i `persons.component.ts` med `forkJoin`.
- **Reception: UI och statistik** – avgör om återkallade biljetter ska visas, utöka statistik med antal pass och uppdelning per dag.
- **Reception: alternativ verifiering** – definiera flöde för verifiering när besökaren saknar QR-biljett.
- **Taggar i grundflöde** – låt arrangör sätta taggar redan i grunduppgifter.
- **Import/export** – lägg till stöd för sidor och taggar.
