# ADR: Laganmälningar Fas 2 (UC-TM005–UC-TM007)

**Datum:** 2026-05-09
**Status:** Beslutad

---

## Kontext

Fas 1 implementerade backend och admin-UI för grundläggande laganmälningar (UC-TM001–UC-TM004): event kan konfigureras för laganmälning, captains kan anmäla lag, och admin kan bekräfta eller avboka anmälningar.

Fas 2 kompletterar flödet med tre delar:

1. **R-TM04** – Arrangör tilldelar ett bekräftat lag till en specifik session, t.ex. "Lag Alpha spelar i sal 3 fredag 14:00".
2. **R-TM05** – Captain ser sina tilldelade lagsessioner i sitt personliga tidschema via samma vy som organisatörssessioner och staffpass.
3. **R-TM06** – Captain kan anmäla lag direkt i den publika appen utan att gå via admin.

---

## Beslut

### Domänlager – Event BC: `TeamSessionAssignment`

Ny entitet `TeamSessionAssignment` placeras som en collection på `Session`-entiteten i Event BC. Inga nya aggregatrötter; `Session` utökas med en lista av tilldelningar.

```
TeamSessionAssignment
  SessionId                    (SessionId – ingår i sammansatt PK)
  TeamEventRegistrationId      (Guid – krångelfri korskontext-referens till Registration BC)
  AssignedAt                   (DateTimeOffset)
  AssignedByPersonId           (PersonId)
```

`TeamEventRegistrationId` lagras som `Guid`, inte som stark id-typ, för att undvika cirkulärt beroende mellan Event BC och Registration BC. Applikationslagret ansvarar för att brygga typerna.

**Ny collection på `Session`:**
```csharp
private readonly List<TeamSessionAssignment> _teamAssignments = [];
public IReadOnlyList<TeamSessionAssignment> TeamAssignments => _teamAssignments.AsReadOnly();
```

**Interna metoder på `Session`:**
```csharp
internal TeamSessionAssignment AssignTeam(Guid registrationId, PersonId assignedById)
internal void RemoveTeamAssignment(Guid registrationId)
```

Invarianter:
- Sessionen måste vara `Active` – annars kastas `SessionInactiveCannotEditException`.
- En registrering kan bara tilldelas en gång per session – dubblettskydd via unikt index på `(session_id, team_event_registration_id)`.
- `RemoveTeamAssignment`: tilldelningen måste finnas – annars kastas `TeamAssignmentNotFoundException`.

**Publika metoder på `Event`-aggregatet (delegerar till Session):**
```csharp
public TeamSessionAssignment AssignTeamToSession(SessionId sessionId, Guid registrationId, PersonId assignedById)
public void RemoveTeamFromSession(SessionId sessionId, Guid registrationId, PersonId performedById)
```

Domain events (lyfts från `Event`):
- `TeamAssignedToSession { EventId, SessionId, TeamEventRegistrationId, AssignedByPersonId, OccurredAt }`
- `TeamRemovedFromSession { EventId, SessionId, TeamEventRegistrationId, RemovedByPersonId, OccurredAt }`

---

### Applikationslager – R-TM04

#### `AssignTeamToSessionCommand`
```
AssignTeamToSessionCommand(EventId, SessionId, TeamEventRegistrationId)
```
Handler-logik:
1. Hämta `TeamEventRegistration` (via `ITeamEventRegistrationRepository`) – validera att status är `Confirmed`; annars `422 Unprocessable Entity`.
2. Verifiera att registreringens `EventId` matchar det angivna `EventId` – annars `422`.
3. Hämta `Event` (med sessioner inkl. TeamAssignments) via `IEventRepository`.
4. Kontrollera att utföraren är admin eller kategoriansvarig (via `IConventionRepository`/Edition).
5. Anropa `event.AssignTeamToSession(sessionId, registrationId.Value, currentUser.PersonId)`.
6. Spara `Event`.

#### `RemoveTeamFromSessionCommand`
```
RemoveTeamFromSessionCommand(EventId, SessionId, TeamEventRegistrationId)
```
Samma behörighetscheck som ovan; anropar `event.RemoveTeamFromSession(...)`.

#### `ListTeamAssignmentsForSessionQuery`
```
ListTeamAssignmentsForSessionQuery(EventId, SessionId)
→ IReadOnlyList<TeamSessionAssignmentDto>
```
Ny DTO:
```csharp
public record TeamSessionAssignmentDto(
    Guid TeamEventRegistrationId,
    string TeamName,
    Guid CaptainPersonId,
    string CaptainName,
    DateTimeOffset AssignedAt);
```
Implementeras i nytt repository-interface (se nedan) – join mot `teams` och `persons` för namn.

---

### Applikationslager – R-TM05

Ny metod på `IMyScheduleRepository`:
```csharp
Task<IReadOnlyList<MyTeamAssignedSessionDto>> ListMyTeamAssignedSessionsAsync(
    PersonId personId, EditionId editionId, CancellationToken ct = default);
```

Ny DTO i `RegistrationDtos.cs`:
```csharp
public record MyTeamAssignedSessionDto(
    Guid SessionId,
    string TeamName,
    string EventTitle,
    DateTime Start,
    DateTime End,
    string VenueName);
```

Ny Query + Handler:
```
GetMyTeamAssignedSessionsQuery(EditionId)
→ IReadOnlyList<MyTeamAssignedSessionDto>
```

Implementeringslogik i `MyScheduleRepository`:
1. Hämta alla `Team`-poster där `CaptainPersonId = personId` och `EditionId = editionId`.
2. Hämta `TeamEventRegistrations` för dessa team med `Status = Confirmed`.
3. Hämta `TeamSessionAssignments` för de bekräftade registreringarna.
4. Join mot `sessions`, `events`, `venues` för tidpunkt och lokal.
5. Returnera sorterat på `Start`.

---

### Applikationslager – R-TM06

Inga nya backend-komponenter behövs – API:t (`POST /api/events/{id}/team-registrations`) är implementerat sedan Fas 1. R-TM06 är en **frontend-uppgift**.

**Publik app:**
- Ny route: `/events/:id/register-team`
- Ny komponent: `team-registration.component.ts` i `projects/public/src/app/features/events/`
- Formulär: lagnamn (required, max 200), submit-knapp
- På success: naviguera till bekräftelsesida eller tillbaka till eventdetalj med bekräftelsemeddelande
- Felhantering: 422 (t.ex. dubbel anmälan) visas som inline-meddelande

---

### Infrastrukturlager

#### Ny EF-konfiguration: `TeamSessionAssignmentConfiguration`

Separat `IEntityTypeConfiguration<TeamSessionAssignment>` (i `EventConfiguration.cs`-filen):
- Tabell: `team_session_assignments`
- Sammansatt PK: `(session_id, team_event_registration_id)`
- FK: `session_id → sessions(id)` med `CascadeDelete`
- `assigned_by_person_id`: konverteras via `PersonId`-konverterare
- Index: `IX_team_session_assignments_registration_id` på `team_event_registration_id` (för schemaqueryn)

`SessionConfiguration` utökas med:
```csharp
builder.HasMany(s => s.TeamAssignments)
    .WithOne()
    .HasForeignKey(t => t.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
builder.Navigation(s => s.TeamAssignments).HasField("_teamAssignments");
```

#### Nytt interface: `ITeamSessionAssignmentRepository`

```csharp
public interface ITeamSessionAssignmentRepository
{
    Task<IReadOnlyList<TeamSessionAssignmentDto>> ListBySessionIdAsync(
        SessionId sessionId, CancellationToken ct = default);
}
```

Implementering i `TeamSessionAssignmentRepository`: join mot `team_event_registrations`, `teams`, `persons`.

#### Migration: `AddTeamSessionAssignments`

#### `MyScheduleRepository` utökas

Ny metod `ListMyTeamAssignedSessionsAsync` som beskrivs ovan.

---

### API-lager

```
POST   /api/events/{eventId}/sessions/{sessionId}/team-assignments            → AssignTeamToSessionCommand    [Authenticated]
DELETE /api/events/{eventId}/sessions/{sessionId}/team-assignments/{regId}    → RemoveTeamFromSessionCommand   [Authenticated]
GET    /api/events/{eventId}/sessions/{sessionId}/team-assignments            → ListTeamAssignmentsForSessionQuery [Authenticated]
GET    /api/schedule/team-sessions?editionId={editionId}                      → GetMyTeamAssignedSessionsQuery [Authenticated]
```

Endpoints registreras i `TeamRegistrationEndpoints.cs` (befintlig fil utökas).

---

### Frontend (admin-app) – R-TM04

I befintlig laganmälningslista (komponent från R-TM03b):
- För varje Confirmed-anmälan: "Tilldela session"-knapp som öppnar en dialog/dropdown med upplagens aktiva sessioner.
- Dialogen listar sessioner med tidpunkt och lokal; val triggar `POST`-endpointen.
- Befintliga tilldelningar visas per session i sessiondetaljvyn (ny sektion "Tilldelade lag").

---

## Motivering

- `TeamSessionAssignment` som entity på `Session` (inom Event-aggregatet) håller skriv- och läsoperationer inom ett aggregat och undviker ett separat aggregat för en relation som är naturligt ägt av en session.
- `TeamEventRegistrationId` lagras som `Guid` (inte stark id-typ) i Event BC för att bryta det cirkulära beroendet Event ↔ Registration. Strängt skrivet vid korsningspunkten i applikationslagret.
- Schedule-queryn (R-TM05) implementeras som en ny metod på befintligt `IMyScheduleRepository`-interface – konsekvent med hur organizer-sessioner och staffpass hanteras.
- R-TM06 kräver inget nytt backend-arbete; backend-API:t är klart från Fas 1.

---

## Bounded contexts som påverkas

| BC | Förändring |
|----|-----------|
| Event | Ny `TeamSessionAssignment`-entitet + collection på `Session`; två nya metoder på `Event`-aggregatet; nya domain events |
| Application | Tre nya commands/handlers (Assign, Remove, List); ny query (GetMyTeamAssigned); `IMyScheduleRepository` utökas |
| Infrastructure | Ny EF-konfiguration + migration; `MyScheduleRepository` utökas; nytt repository för session assignments |
| API | Tre nya endpoints + en ny schedule-endpoint |
| Frontend (admin) | Utökad laganmälningslista + sessiondetalj |
| Frontend (public) | Ny registreringskomponent för captain |

---

## Risker

- **Eager loading av TeamAssignments**: `IEventRepository.GetByIdAsync` måste inkludera `Sessions.TeamAssignments` för att domänmetoderna ska fungera. Om Event-aggregatet laddas utan Include kommer `_teamAssignments`-listan att vara tom och dubblettkontroll i domänen missar existerande tilldelningar. Kontrollera och utöka Include-kedjan i `EventRepository`.
- **Cross-BC query i MyScheduleRepository**: Queryn joinar `teams`, `team_event_registrations`, `team_session_assignments`, `sessions`, `events`, `venues` – sex tabeller. Vid hög trafik bör ett index på `teams.captain_person_id` och `teams.edition_id` finnas (skapades i Fas 1-migrationen).
- **Inga lagmedlemmar utöver captain**: R-TM05 returnerar bara sessioner för captains. Om lagmedlemmar läggs till i framtiden behöver queryn utökas.

---

## Acceptanskriterier

### UC-TM005 – Tilldela lag till session
- [ ] `TeamSessionAssignment`-entitet med sammansatt PK; `Session.AssignTeam()` kastar vid inaktiv session; enhetstest
- [ ] Dubblettilldelning (samma lag, samma session) kastar; enhetstest
- [ ] `RemoveTeamAssignment` för icke-existerande tilldelning kastar; enhetstest
- [ ] `AssignTeamToSessionCommand` + handler; anmälan måste vara Confirmed – annars 422
- [ ] Anmälan måste tillhöra rätt evenemang – annars 422
- [ ] Saknad behörighet → 403
- [ ] `RemoveTeamFromSessionCommand` + handler med samma behörighetsregler
- [ ] Migration skapar tabell `team_session_assignments` med rätt index
- [ ] Admin-UI: "Tilldela session"-funktion för Confirmed-lag

### UC-TM006 – Lagkaptenens tidschema
- [ ] `IMyScheduleRepository.ListMyTeamAssignedSessionsAsync` returnerar sessioner för inloggad captain
- [ ] Returnerar tom lista om captain saknar Confirmed-registreringar eller om inga sessioner är tilldelade
- [ ] `GET /api/schedule/team-sessions` implementerat och returnerar korrekt data
- [ ] Enhetstester för handler

### UC-TM007 – Captain anmäler lag (publik app)
- [ ] Ny route `/events/:id/register-team` i publik app
- [ ] Formulär med lagnamn; submit anropar befintligt backend-API
- [ ] Framgång: bekräftelsemeddelande visas
- [ ] Fel (422): inline-felmeddelande visas
- [ ] Länk till registreringsformuläret visas på eventdetalj när `registrationMode = Team`
