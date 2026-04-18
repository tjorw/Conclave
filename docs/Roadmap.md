# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

---

## Implementationsordning

Prioriterad lista – ej startade överst, klara underst.

- [x] `R15` Biljettmodell reviderad – `validDays`, `allowedCategories`, `TicketPerk` (UC-TK001/TK002)
- [ ] `R16` Biljettlivscykel reviderad – manuell betalning, webhook, innehavaravbokning (UC-TK003–TK006)
- [ ] `R17` Makuleringskaskad + uthämtning med förmåner (UC-TK007/TK008)
- [ ] `R18` `RegistrationRuleService.ValidateTicket` med dag- och kategorivalidering (UC-TK009)
- [ ] `R20` Centralisera admin-claimvärde (`"true"`) i auth-konstanter
- [ ] `R21` Centralisera fallback för frontend-URL i auth-flöden
- [ ] `R22` Centralisera JWT-konfigurationsnycklar (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`)
- [x] `R23` Self-service registration använder server-side `ICurrentUser` (tar bort klientstyrt `PersonId`)
- [ ] `R11` Fas 4.1 Demo-deploy med fiktivt konvent

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).

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
| **Fel exception-typ i Registration Application** | Registration-handlers kastar brett `InvalidOperationException` i stället för semantiska typer enligt `Backend.md` (`ResourceNotFoundException`, `ForbiddenException`, `DomainRuleViolationException`). Detta försvagar API-kontrakt, error mapping och observability. | **Hög – standardisera exceptions enligt riktlinje** |
| **`Shift` saknar `EditionId`** | `Shift` har ingen direkt koppling till `EditionId`. `MyScheduleRepository` löser detta via `Edition.Stations`-navigeringen (shadow FK). Om Shift-kontexten växer bör ett direkt `EditionId` övervägas på `Shift` för att slippa join-beroendet mot Convention. | Låg – fungerar korrekt, men fragil vid schemamigration |
| **Deduplikering i tidsschema** | Om samma session förekommer i flera kategorier (t.ex. bokad OCH arrangör) prioriteras Booked > Organiser > Watching i `MyScheduleRepository`. Prioriteringslogiken är inte testad på domännivå. Om affärsreglerna ändras (t.ex. "visa alltid arrangörsrollen oavsett bokning") behöver deduplikeringen ses över. | Låg – nuvarande beteende är rimligt |
| **Inga `DbSet<Station>` i `ConventionDbContext`** | `Station` och `Venue` nås via `db.Set<T>()` i stället för namngivna `DbSet<T>`-properties. Inkonsekvens mot övriga entiteter. Lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext` om fler queries börjar hämta dem direkt. | Låg |
| **R19: Byt literal `"IsAdmin"` till `AuthConstants.Policies.IsAdmin` i Registration-endpoints** | `RegistrationEndpoints` använder policy-namnet som hårdkodad sträng på flera ställen medan övriga endpoints använder konstant. Standardisera till konstant för compile-time-säkerhet och enklare refaktorering. | **Hög – liten ändring med hög riskreduktion** |
| **R20: Centralisera admin-claimvärde (`"true"`)** | Samma claimvärde hårdkodas vid både token-utgivning och policykontroll. Inför en gemensam konstant (t.ex. i `AuthConstants`) så att claim-kontraktet inte divergerar. | Medel |
| **R21: Centralisera fallback för frontend-URL i auth-flöden** | Default-värdet `http://localhost:4201` upprepas i flera auth-endpoints. Flytta till en gemensam konstant eller options-klass för att undvika inkonsekvent miljökonfiguration. | Medel |
| **R22: Centralisera JWT-konfigurationsnycklar** | Nycklarna `Jwt:Key`, `Jwt:Issuer` och `Jwt:Audience` används duplicerat i startup och auth. Samla i konstanter/options för att minska typo-risk och förenkla ändringar. | Medel |
| **Gamla TK-implementationer behöver revideras** | R15 är genomförd (TicketType med `validDays`/`allowedCategories`/`TicketPerk`). Kvar att slutföra: betalningsflödet (R16), makuleringskaskad och uthämtning med förmåner (R17), samt full dag-/kategorivalidering i `RegistrationRuleService.ValidateTicket` (R18). | **Hög – blockar korrekt sessionsvalidering tills R16–R18 är klara** |

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