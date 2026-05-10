# ADR: Flerspråksstöd (R-I18N01–R-I18N05)

**Datum:** 2026-05-10  
**Status:** Godkänd

---

## Kontext

Conclave har idag enbart stöd för svenska. Innehåll — informationssidor och evenemangsbeskrivningar — är hårdkodat på ett enda språk. Publika appens UI-texter är en blandning av hårdkodade strängar i templates och partiellt extraherade labels-filer.

Kraven (UC-I18N001–UC-I18N004) specificerar:
1. Admin aktiverar vilka språk som gäller per upplaga.
2. Admin/arrangör redigerar översättningar av sidor och evenemang.
3. Publika appen visar rätt språk baserat på besökarens val, med tyst fallback till primärspråk.
4. UI-texter i publika appen ska vara översättningsbara.

Admin- och reception-appen behöver inte byta språk — de förblir svenska.

---

## Beslut

### 1. Locale-representation

Locale representeras som en **BCP 47 2-bokstavskod** (`string`): `"sv"`, `"en"`.

- Inte ett enum — lättare att utöka utan domänmigration.
- Stödda locales i fas 1: `{ "sv", "en" }` — definieras som konstant i domänen (`LocaleConstants.SupportedLocales`).
- Valideras i applikationslagret vid inkommande kommandon.
- Lagras i lowercase.

### 2. `EditionLocale` — aktiverade språk per upplaga

`EditionLocale` är en **child-entitet** på `Edition`-aggregatet med naturlig nyckel `(EditionId, Locale)`.

```
EditionLocale
  EditionId   : EditionId  (FK)
  Locale      : string     (PK del 1)
  IsPrimary   : bool
```

Ny metod på `Edition`:

```csharp
Edition.ConfigureLocales(
    IReadOnlyList<string> locales,
    string primaryLocale,
    PersonId performedById)
```

Invarianter:
- `locales` ⊆ `LocaleConstants.SupportedLocales` — annars `ArgumentException`.
- Exakt ett primärspråk. `primaryLocale` ∈ `locales` — annars `ArgumentException`.
- Metoden ersätter hela samlingen (wholesale replace) — enkel, deterministisk semantik.

Domain events:
- `EditionLocalesConfigured(editionId, locales, primaryLocale, occurredAt)`

EF Core:
- Tabell: `edition_locales(edition_id, locale, is_primary)`.
- Sammansatt primärnyckel `(edition_id, locale)`.
- Migration: `AddEditionLocales`.

Nya applikationskomponenter:
- `SetEditionLocalesCommand(EditionId, Locales[], PrimaryLocale)` + handler.
- `GetEditionLocalesQuery(EditionId)` + handler → `EditionLocaleDto[]`.
- Endpoint `PUT /api/editions/{id}/locales` (admin).
- Endpoint `GET /api/editions/{id}/locales` (authenticated).

### 3. `PageTranslation` — översättning av informationssidor

`PageTranslation` är en **child-entitet** på `Page`-aggregatet.

```
PageTranslation
  PageId   : PageId   (FK, PK del 1)
  Locale   : string   (PK del 2)
  Title    : string
  Content  : string
```

Ny metod på `Page`:

```csharp
Page.SetTranslation(string locale, string title, string content)
```

Semantik: **upsert** — om `(PageId, Locale)` finns uppdateras den, annars skapas den.

Invarianter:
- `locale` måste vara i `LocaleConstants.SupportedLocales`.
- Primärspråkets innehåll ändras aldrig via `SetTranslation`.
- `Title` max 300 tecken, `Content` max 20 000 tecken — samma gränser som originalet.

EF Core:
- Tabell: `page_translations(page_id, locale, title, content)`.
- Sammansatt primärnyckel `(page_id, locale)`.
- Migration: `AddPageTranslations`.

Nya applikationskomponenter:
- `SetPageTranslationCommand(PageId, Locale, Title, Content)` + handler.
- `GetPageTranslationQuery(PageId, Locale)` + handler → `PageTranslationDto?`.
- Endpoint `PUT /api/pages/{id}/translations/{locale}` (admin).
- Endpoint `GET /api/pages/{id}/translations/{locale}` (admin).
- `IPageRepository` utökas med `GetTranslationAsync(pageId, locale, ct)`.

### 4. `EventTranslation` — översättning av evenemangsbeskrivningar

`EventTranslation` är en **child-entitet** på `Event`-aggregatet.

```
EventTranslation
  EventId     : EventId  (FK, PK del 1)
  Locale      : string   (PK del 2)
  Title       : string
  Description : string
```

Ny metod på `Event`:

```csharp
Event.SetTranslation(string locale, string title, string description)
```

Semantik: **upsert**.

Invarianter:
- `locale` ∈ `LocaleConstants.SupportedLocales`.
- `Description` max 10 000 tecken — samma gräns som originalet.
- Arrangör kan bara sätta translation för ett evenemang hen äger.

EF Core:
- Tabell: `event_translations(event_id, locale, title, description)`.
- Sammansatt primärnyckel `(event_id, locale)`.
- Migration: `AddEventTranslations`.

Nya applikationskomponenter:
- `SetEventTranslationCommand(EventId, Locale, Title, Description)` + handler.
- `GetEventTranslationQuery(EventId, Locale)` + handler → `EventTranslationDto?`.
- Endpoint `PUT /api/events/{id}/translations/{locale}` (admin eller arrangör).
- Endpoint `GET /api/events/{id}/translations/{locale}` (admin eller arrangör).
- `IEventRepository` utökas med `GetTranslationAsync(eventId, locale, ct)`.

### 5. Fallback-logik i publika queries

Alla publika queries som returnerar innehåll utökas med `string? locale = null`.

**Fallback-kedja:**
```
locale angiven + translation finns → returnera translation
locale angiven + translation saknas → returnera originalinnehåll (tyst)
locale = null → returnera originalinnehåll
```

Aldrig ett fel — fallback är alltid tyst.

Berörda queries (utökas med `Locale?`-parameter):
- `GetPublicPageQuery` → `GetPublishedBySlugAsync` utökas; returnerar `PageTranslation` om tillgänglig.
- `ListPublicMenuPagesQuery` → menu-titlar ersätts med translation om tillgänglig.
- Publika event-queries (om de existerar) — samma mönster.

API-parametrar läggs till som **query-parameter** `?locale=en`:
- `GET /api/public/pages/{slug}?locale=en`
- `GET /api/public/pages/menu?locale=en`
- `GET /api/public/events?locale=en` (om sådan endpoint finns)

**Inte** via `Accept-Language`-header på API-nivå — query-param är lättare att cacha och dela som URL.

### 6. R-I18N01 — Språkstyrning i publika appen

**Vad som behöver göras:**

Publika appens UI-texter (knappar, rubriker, placeholder-texter, systemmeddelanden) extraheras till ett tvåspråkigt labels-system.

**Mönster:**

Ny `LocaleService` i publik app:

```typescript
// public/src/app/services/locale.service.ts
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly _locale = signal(this.initLocale());
  readonly locale = this._locale.asReadonly();

  setLocale(locale: string): void {
    this._locale.set(locale);
    localStorage.setItem('preferred_locale', locale);
  }

  private initLocale(): string {
    const stored = localStorage.getItem('preferred_locale');
    if (stored && SUPPORTED_LOCALES.includes(stored)) return stored;
    const browser = navigator.language.split('-')[0];
    if (SUPPORTED_LOCALES.includes(browser)) return browser;
    return 'sv';
  }
}

export const SUPPORTED_LOCALES = ['sv', 'en'] as const;
```

Ny `LabelsService` i publik app:

```typescript
// public/src/app/services/labels.service.ts
@Injectable({ providedIn: 'root' })
export class LabelsService {
  private readonly locale = inject(LocaleService);
  readonly ui = computed(() => this.locale.locale() === 'en' ? EN_UI : SV_UI);
  readonly pages = computed(() => this.locale.locale() === 'en' ? EN_PAGES : SV_PAGES);
  readonly errors = computed(() => this.locale.locale() === 'en' ? EN_ERRORS : SV_ERRORS);
}
```

Label-filer: `public/src/app/labels/sv/` och `public/src/app/labels/en/`.

Komponenter injicerar `LabelsService` och läser `labels.ui().someKey` i stället för hårdkodad text.

**Scope för R-I18N01:**
- Skapa `LocaleService` och `LabelsService` i publik app.
- Skapa `sv`- och `en`-label-objekt för publik apps UI-texter.
- Uppgradera alla publika komponenter att använda `LabelsService` i stället för hårdkodade strängar.
- Admin- och reception-appen: verifiera att befintliga label-filer täcker alla strängar — men ingen locale-switching.

### 7. R-I18N05 — Publik språkväljare

`LanguageSelectorComponent` i publika appens shell:

```typescript
// Standalone component
// Hämtar aktiverade locales för aktiv upplaga från API: GET /api/editions/{id}/locales
// Visar dropdown/toggle med aktiverade locales
// Anropar LocaleService.setLocale() vid val
// Döljer sig om bara ett språk är aktiverat
```

Publika appens services utökas: alla anrop som returnerar innehåll lägger till `?locale=${localeService.locale()}` som query-parameter via en HTTP-interceptor eller per-service-metod.

**Rekommendation:** per-service-metod (inte interceptor) — tydligare vad som faktiskt skickas, undviker oavsiktliga locale-headers på auth-anrop.

---

## Motivering

**Varför string och inte enum för locale?**  
Enums kräver ny kodomkompilering + DB-migration för varje nytt språk. String med explicit validering i applikationslagret är lika säkert och mer flexibelt.

**Varför wholesale replace på `ConfigureLocales` och inte add/remove?**  
Admin konfigurerar lokaler som en helhet — exakt ett primärspråk är en invariant som lättast hålls i en atomär operation. Add/remove-semantik ökar risken för inkonsistenta mellanlägen.

**Varför child-entitet på aggregat, inte eget aggregat?**  
`PageTranslation` saknar livscykel utanför `Page` — den publiceras inte separat och existerar inte utan sin sida. Samma gäller `EventTranslation`. Att äga dem i aggregatet håller domänreglerna (t.ex. max-tecken) lokalt och undviker inter-aggregat-koordination.

**Varför query-param och inte Accept-Language header?**  
Headers cachebryter inte automatiskt — samma URL men annan header returnerar ibland samma cachat svar. Query-param `?locale=en` är explicit, cacherbar och delbar som URL. `Accept-Language` används bara som fallback vid initiering av `LocaleService` i webbläsaren.

**Varför inte `@angular/localize`?**  
`@angular/localize` kräver separata builds per locale — onödig komplexitet när antalet locales är litet och innehållet är dynamiskt (per upplaga). En signal-baserad `LabelsService` ger samma resultat utan build-overhead och fungerar med runtime locale-switching.

**Varför skapa `LocaleService` i R-I18N01 och inte R-I18N05?**  
`LocaleService` är en förutsättning för att labels-systemet ska fungera korrekt. Att skapa den i R-I18N01 gör att R-I18N02–R-I18N04 kan använda den för att lägga till locale-param i API-anrop progressivt utan att blockeras på R-I18N05.

---

## Bounded contexts och filer som påverkas

| Vad | Fil/komponent |
|---|---|
| `LocaleConstants` | `Domain/Shared/LocaleConstants.cs` (ny) |
| `EditionLocale` entitet | `Domain/Convention/Entities/EditionLocale.cs` (ny) |
| `Edition.ConfigureLocales()` | `Domain/Convention/Aggregates/Edition.cs` |
| `EditionLocalesConfigured` domain event | `Domain/Convention/Events/EditionLocaleEvents.cs` (ny) |
| EF Core-konfiguration EditionLocale | `Infrastructure/Persistence/Configurations/Convention/EditionLocaleConfiguration.cs` (ny) |
| Migration | `AddEditionLocales` |
| `SetEditionLocalesCommand` + handler | `Application/Convention/Commands/SetEditionLocales/` (ny) |
| `GetEditionLocalesQuery` + handler | `Application/Convention/Queries/GetEditionLocales/` (ny) |
| Endpoints edition locales | `Api/Endpoints/EditionEndpoints.cs` |
| `PageTranslation` entitet | `Domain/Content/Entities/PageTranslation.cs` (ny) |
| `Page.SetTranslation()` | `Domain/Content/Aggregates/Page.cs` |
| EF Core-konfiguration PageTranslation | `Infrastructure/Persistence/Configurations/Content/PageTranslationConfiguration.cs` (ny) |
| Migration | `AddPageTranslations` |
| `SetPageTranslationCommand` + handler | `Application/Content/Commands/SetPageTranslation/` (ny) |
| `GetPageTranslationQuery` + handler | `Application/Content/Queries/GetPageTranslation/` (ny) |
| `GetPublicPageHandler` | utökas med locale + fallback |
| `ListPublicMenuPagesHandler` | utökas med locale + fallback |
| `IPageRepository` | `GetTranslationAsync()` (ny metod) |
| `PageRepository` | implementation av ny metod |
| Endpoints page translations | `Api/Endpoints/PageEndpoints.cs` |
| `EventTranslation` entitet | `Domain/Event/Entities/EventTranslation.cs` (ny) |
| `Event.SetTranslation()` | `Domain/Event/Aggregates/Event.cs` |
| EF Core-konfiguration EventTranslation | `Infrastructure/Persistence/Configurations/Event/EventTranslationConfiguration.cs` (ny) |
| Migration | `AddEventTranslations` |
| `SetEventTranslationCommand` + handler | `Application/Event/Commands/SetEventTranslation/` (ny) |
| `GetEventTranslationQuery` + handler | `Application/Event/Queries/GetEventTranslation/` (ny) |
| `IEventRepository` | `GetTranslationAsync()` (ny metod) |
| `EventRepository` | implementation av ny metod |
| Endpoints event translations | `Api/Endpoints/EventEndpoints.cs` |
| `LocaleService` (publik app) | `public/src/app/services/locale.service.ts` (ny) |
| `LabelsService` (publik app) | `public/src/app/services/labels.service.ts` (ny) |
| Label-filer sv + en (publik app) | `public/src/app/labels/sv/*.ts` och `en/*.ts` (nya) |
| `LanguageSelectorComponent` | `public/src/app/shared/language-selector/` (ny) |
| Publika shell-komponenten | utökas med `LanguageSelectorComponent` |
| Publika tjänster (service-anrop) | locale-parameter på content-anrop |

---

## Risker

| Risk | Sannolikhet | Åtgärd |
|---|---|---|
| Fallback-logik exponerar partiellt översatt innehåll | Medel | Dokumenterat beteende; primärspråk är alltid komplett |
| Edition utan locale-konfiguration när queries körs | Låg | Queries behandlar null locale som "returnera original" — ingen konfiguration krävs |
| EF Core composite key för owned collection | Låg | Väletablerat mönster; konfigureras explicit i `OnModelCreating` |
| Publika app-komponenter missar locale-param | Medel | Genomgång komponent för komponent i R-I18N05; integrationstester |
| `@angular/localize`-konflikt om det installeras senare | Låg | Vår `LabelsService` lever parallellt med `@angular/localize` utan konflikt |
| R-I18N01 scope-kryp (audit tar lång tid) | Hög | Avgränsa till publika appen; admin-labels är redan extraherade |

---

## Acceptanskriterier

### R-I18N01
- [ ] `LocaleService` och `LabelsService` skapade i publik app
- [ ] Label-objekt för `sv` och `en` skapade för publik apps UI-texter
- [ ] Inga hårdkodade svenska UI-strängar kvar i publika appens templates

### R-I18N02
- [ ] `Edition.ConfigureLocales()` kan sätta locales med exakt ett primärspråk
- [ ] Okänd locale ger `ArgumentException`
- [ ] Mer än ett primärspråk ger `ArgumentException`
- [ ] `GET /api/editions/{id}/locales` returnerar aktiva locales
- [ ] `PUT /api/editions/{id}/locales` kräver admin-behörighet
- [ ] Enhetstest för `Edition.ConfigureLocales()` — lyckligt flöde och båda felfall
- [ ] Handlertester för `SetEditionLocalesCommand`

### R-I18N03
- [ ] `Page.SetTranslation()` skapar eller uppdaterar `PageTranslation` för angiven locale
- [ ] `GetPublicPageQuery` returnerar `PageTranslation.Title` och `PageTranslation.Content` när locale matchar
- [ ] `GetPublicPageQuery` returnerar originalinnehåll om ingen translation finns — utan fel
- [ ] `PUT /api/pages/{id}/translations/{locale}` kräver admin-behörighet
- [ ] Enhetstest för `Page.SetTranslation()`
- [ ] Handlertester för `SetPageTranslationCommand` och `GetPublicPageHandler` (med och utan translation)

### R-I18N04
- [ ] `Event.SetTranslation()` skapar eller uppdaterar `EventTranslation`
- [ ] Arrangör kan bara redigera translation för egna evenemang
- [ ] Publika event-queries returnerar `EventTranslation`-innehåll när locale matchar, annars original
- [ ] Enhetstest för `Event.SetTranslation()`
- [ ] Handlertester

### R-I18N05
- [ ] `LanguageSelectorComponent` visas i publika appens shell
- [ ] Komponenten visar bara locales aktiverade för aktiv upplaga
- [ ] Localepreferens sparas i `localStorage` och återläses vid nästa besök
- [ ] Webbläsarens `Accept-Language` används som fallback om inget localStorage-värde finns
- [ ] Alla publika API-anrop inkluderar `?locale=` när locale är satt
- [ ] Komponenten döljer sig om bara ett språk är aktiverat
