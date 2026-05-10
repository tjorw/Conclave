# ADR: Övrig backlog (R-OB01–R-OB03)

**Datum:** 2026-05-09  
**Status:** Godkänd

---

## Kontext

Roadmap-avsnittet "Övrig backlog" innehåller sex poster av varierande storlek.
Analysen visar att två av dem redan är implementerade:

- **Taggar i grundflöde** — `events.component.ts` har redan ett `programTags`-fält i
  `createForm`, backend tar emot dem i `CreateEventCommand`. Markeras `[x]`.
- **Import/export taggar** — `EditionExportDocument` v2 innehåller redan
  `programTagDefinitions` och `ImportEditionHandler` skapar dem vid import. Markeras `[x]`.

Kvar att implementera:

1. **Debounced sökning** – `createSearchStream()`-helper i reception-appen
2. **forkJoin** – `persons.component.ts` manuell pending-räknare
3. **Reception: statistik** – dagantal + återkallade biljetter
4. **Reception: alternativ verifiering** – visuell ledning i checkin-vyn
5. **Import/export sidor** – `Page`-poster i exportdokumentet

---

## Beslut

### R-OB01 – Frontend-refaktorer

**Debounced sökning**

Extrahera en gemensam `createSearchStream<T>()` i
`frontend/projects/reception/src/app/shared/search-stream.ts`:

```typescript
function createSearchStream<T>(
  control: AbstractControl,
  fetch: (term: string) => Observable<T[] | null>,
  options?: { minLength?: number; debounce?: number }
): Observable<T[] | null>
```

- `minLength` default 2, `debounce` default 300 ms
- Returnerar `null` vid för kort term (konsumentens signal sätts inte)
- Används av `checkin.component.ts` och `walkup.component.ts` (identisk pipeline dupliceras idag)

**forkJoin i persons.component.ts**

Ersätt den manuella nedräknaren (`let pending = 3; if (--pending === 0)`) i
`loadEditionRoles()` med:

```typescript
forkJoin({
  visitors:  this.svc.listEditionVisitors(editionId),
  organisers: this.svc.listEditionOrganisers(editionId),
  staff:     this.svc.listEditionStaff(editionId),
}).pipe(
  catchError(() => of({ visitors: [], organisers: [], staff: [] }))
).subscribe(({ visitors, organisers, staff }) => {
  this.editionRolesMap.set(this.buildRoleMap(visitors, organisers, staff));
  this.rolesLoading.set(false);
});
```

Fördel: inga partiella resultat om ett anrop failar, tydligare semantik.

---

### R-OB02 – Reception: statistik och alternativ verifiering

**Statistik: dagantal i schedule-panel**

`PersonScheduleDto.dailySummary` innehåller redan `shiftCount` och
`sessionCount` per dag men de visas inte i `schedule-panel.component.html`.
Lägg till en subtextsrad i dag-headern:

```
Lördag 10 maj  ·  3 h 45 min
  2 pass · 1 session
```

Ingen backend-förändring — datan finns.

**Återkallade biljetter**

Biljetter med status `Revoked` visas idag utan incheckningsknapp (korrekt).
Lägg till ett tydligt informationsblock i `ticket-card.component.html` när
`status === 'Revoked'`:

> "Biljetten är återkallad och kan inte checkas in."

Inga andra förändringar i logiken.

**Alternativ verifiering**

"Alternativ verifiering" innebär att besökaren saknar QR-biljett. Befintlig
namnssökning täcker detta tekniskt. Åtgärd: lägg till en visuell ledning
(`mat-hint` eller informationstext) i `checkin.component.html`:

> "Har besökaren ingen QR-kod? Sök på namn eller e-post nedan."

Ingen ny backend-endpoint eller flödeslogik behövs.

---

### R-OB03 – Import/export: sidor (Page)

`EditionExportDocument` exporterar idag venues, staffAreas, categories, events,
ticketTypes och programTagDefinitions. Sidor (CMS-innehåll, `Page`) saknas.

**Nytt fält i kontraktet** (schema version 3):

```csharp
public sealed record EditionExportDocument(
    ...
    [property: JsonPropertyName("pages")] IReadOnlyList<ExportPageDto>? Pages = null)
{
    public const int CurrentSchemaVersion = 3;
}

public sealed record ExportPageDto(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("showInPublicMenu")] bool ShowInPublicMenu,
    [property: JsonPropertyName("menuSortOrder")] int MenuSortOrder);
```

**Export** (`ExportEditionHandler` / `IEditionExportReadService`):

Hämtar publicerade `Page`-poster kopplade till `EditionId`. Utkast exporteras
inte.

**Import** (`ImportEditionHandler`):

1. Skapar pages som `Draft` — aldrig publicerade automatiskt.
2. Kontrollerar om slug redan existerar i målupplagans scope
   (`ConventionId`, `EditionId`); varnar och hoppar över
   (`SlugAlreadyExists`-varning) utan att kasta exception.
3. Bevarar `MenuSortOrder` och `ShowInPublicMenu`.

**Bakåtkompatibilitet:**

Import av v1- och v2-dokument fortsätter fungera — `Pages`-fältet är nullable
(`IReadOnlyList<ExportPageDto>?`). `IsSupportedSchemaVersion` utökas med version 3.

**Vad exporteras aldrig:**

- Page-ID:n
- Publikationsdatum
- Konventionstillhörighet

---

## Motivering

**Varför `createSearchStream()` i reception, inte i shared?**  
Mönstret är reaktivt och specifikt för reception-appens `AbstractControl`-sökning.
Admin-appen filtrerar klientside med signals och behöver inte samma pipeline.

**Varför `forkJoin` med `catchError` och inte tre separata subscriptions?**  
Den nuvarande lösningen sätter `rolesLoading` till false om alla tre failar men
visar inga roller. `forkJoin` med fallback ger samma beteende mer tydligt och
eliminerar risken att räknaren fastnar vid partial failure.

**Varför importera sidor som Draft?**  
Automatisk publicering av importerat innehåll är riskabelt — admin bör gå igenom
sidorna i den nya upplagan innan de syns publikt.

**Varför inte exportera utkast-pages?**  
Export av utkast kan ge oönskad informationsläckage vid delning av
exportdokument. Exporten tar därför bara med publicerade pages.

---

## Bounded contexts och filer som påverkas

| Vad | Fil/komponent |
|---|---|
| `createSearchStream()` | `reception/shared/search-stream.ts` (ny) |
| checkin.component.ts | använder `createSearchStream()` |
| walkup.component.ts | använder `createSearchStream()` |
| persons.component.ts | `forkJoin` i `loadEditionRoles()` |
| schedule-panel.component.html | dagantal-rad |
| ticket-card.component.html | återkallad-varningsblock |
| checkin.component.html | alternativ verifiering-ledning |
| `EditionExportDocument` | + `Pages`, schemaversion 3 |
| `ExportPageDto` | nytt kontrakt (ny record) |
| `ExportEditionHandler` / `IEditionExportReadService` | hämtar pages |
| `ImportEditionHandler` | skapar pages som Draft |
| `IEditionExportReadService` | ny metod `GetPagesByEditionIdAsync` (alt. använder `IPageRepository`) |

---

## Risker

| Risk | Åtgärd |
|---|---|
| Import v1/v2-dokument slutar fungera | `Pages` är nullable; `IsSupportedSchemaVersion` accepterar 1, 2 och 3 |
| Slug-kollision vid skapande av ny upplaga | Kollision kontrolleras i målupplagans scope; varning registreras och sidan hoppas över |
| forkJoin-felhantering döljer partiella fel | Varning-signal för rolesError kan läggas till |

---

## Acceptanskriterier

- [x] `createSearchStream()` extraherad; `checkin.component.ts` och
  `walkup.component.ts` använder den; ingen beteendeförändring
- [x] `persons.component.ts` använder `forkJoin`; manuell räknare borttagen
- [x] `schedule-panel.component.html` visar antal pass + sessioner per dag
- [x] `ticket-card.component.html` visar tydlig text för återkallade biljetter
- [x] `checkin.component.html` visar ledning för alternativ verifiering
- [x] `EditionExportDocument` version 3 inkluderar `pages`-fält
- [x] Export inkluderar publicerade pages för vald upplaga och exkluderar utkast
- [x] Import skapar pages som Draft; slug-kollision i målupplagans scope ger varning, inte fel
- [x] Import av v1/v2-dokument fortsätter fungera utan fel
