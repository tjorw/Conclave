# ADR: Kategori- och tagg-översättningar (R-I18N06–R-I18N07)

**Datum:** 2026-05-10  
**Status:** Implementerad

---

## Kontext

Flerspråksstödet i R-I18N01–R-I18N05 täcker sidor, evenemangsbeskrivningar och UI-texter. Publika programflödet visar dock kategorier och programtaggar som alltid returneras på originalspråket (svenska). Besökare som valt engelska ser fortfarande hårdkodade svenska kategori- och taggnamn — exempelvis "Rollspel" och "Familjevänligt" — vilket bryter den språkupplevelse som det övriga i18n-arbetet skapar.

Dessutom ska import/export av upplagedata inkludera alla översättningar, så att en upplaga som exporteras på en instans kan importeras på en annan med bibehållna översättningar.

---

## Beslut

### 1. `CategoryTranslation` — ny child-entitet på `Category`

`CategoryTranslation` är en **child-entitet** på `Category`-entiteten (som i sin tur ägs av `Edition`).

```
CategoryTranslation
  CategoryId : CategoryId  (FK, PK del 1)
  Locale     : string      (PK del 2)
  Name       : string      (max 200 tecken)
```

Ny metod på `Category`:

```csharp
internal void UpsertTranslation(string locale, string name)
```

Semantik: **upsert** — om `(CategoryId, Locale)` finns uppdateras `Name`, annars skapas en ny `CategoryTranslation`.

Invarianter:
- `locale` ∈ `LocaleConstants.SupportedLocales` — annars `ArgumentException`.
- `name` får inte vara tomt.
- `name` max 200 tecken.

Eftersom `Category` är en child-entitet (inte ett aggregat) anropas `UpsertTranslation` via ett nytt kommando som laddar `Edition` → hittar rätt `Category` → delegerar.

Ny metod exponeras på `Edition`:

```csharp
public void SetCategoryTranslation(CategoryId categoryId, string locale, string name, PersonId performedById)
```

Behörighet: admin.

EF Core:
- Tabell: `category_translations(category_id, locale, name, tenant_id)`.
- Sammansatt primärnyckel `(category_id, locale)`.
- Index: `IX_category_translations_category_id`.
- `tenant_id` via shadow property (TenantSeedInterceptor).

### 2. `ProgramTagTranslation` — ny child-entitet på `Edition`

`ProgramTagDefinition` är ett **value object** utan eget ID. För att knyta en översättning till en specifik tagg-definition används den naturliga nyckeln `(EditionId, TagName)`.

```
ProgramTagTranslation
  EditionId       : EditionId  (FK, PK del 1)
  TagName         : string     (PK del 2, max 64 tecken — originalnamnet)
  Locale          : string     (PK del 3)
  TranslatedName  : string     (max 64 tecken)
```

`ProgramTagDefinition` förblir ett value object — ingen refaktorering av befintlig kod.

Ny metod på `Edition`:

```csharp
public void SetProgramTagTranslation(string tagName, string locale, string translatedName, PersonId performedById)
```

Invarianter:
- `tagName` måste matcha en befintlig `ProgramTagDefinition.Name` (case-insensitive).
- `locale` ∈ `LocaleConstants.SupportedLocales`.
- `translatedName` max 64 tecken, ej tomt.

Semantik: upsert på `(EditionId, TagName, Locale)`.

EF Core:
- Tabell: `program_tag_translations(edition_id, tag_name, locale, translated_name, tenant_id)`.
- Sammansatt primärnyckel `(edition_id, tag_name, locale)`.
- Index: `IX_program_tag_translations_edition_id`.
- `tenant_id` via shadow property.
- `ProgramTagTranslation` konfigureras som owned collection på `Edition`.

### 3. Fallback-logik i publika feed-queries

Samma fallback-mönster som för sidor och evenemang (se ADR R-I18N01–R-I18N05):

```
locale angiven + translation finns → returnera translated name
locale angiven + translation saknas → returnera originalnamnet (tyst)
locale = null → returnera originalnamnet
```

**Berörda queries:**

`ListEventsQuery` och `GetEventQuery` (de offentliga API-versionerna) accepterar redan `?locale=` på sikt — men i praktiken behöver vi utöka de befintliga frågorna till att ta med `Locale?` och joina mot `category_translations` och `program_tag_translations`.

Konkret förändring i `IEventRepository`:
- `ListEventsByEditionIdAsync(editionId, locale?, ct)` — utökas med locale-param.
- `GetEventByIdAsync(eventId, locale?, ct)` — utökas med locale-param.

I infrastrukturen: vänsterjoinar mot `category_translations` och `program_tag_translations` med `COALESCE`-logik (eller C#-sida fallback efter hämtning).

**Val: C#-sida fallback** (inte SQL `COALESCE`) — enklare att underhålla och tillräckligt effektivt för de datamängder vi hanterar.

Pattern i read service:

```csharp
var categoryName = categoryTranslationMap.TryGetValue((categoryId, locale), out var catTrans)
    ? catTrans
    : originalCategoryName;

var translatedTags = tags.Select(tag =>
    tagTranslationMap.TryGetValue((editionId, tag, locale), out var tagTrans)
        ? tagTrans
        : tag).ToList();
```

Translations laddas i en separat query per editions-ID, ej per event, för att undvika N+1.

### 4. Import/export — schema version 4

**Schemaversion höjs till 4.** Import-hanteraren stöder fortfarande v1, v2 och v3 (bakåtkompatibelt).

**`ExportCategoryDto`** utökas med valfritt fält:

```json
{
  "name": "Rollspel",
  "translations": [
    { "locale": "en", "name": "Roleplaying" }
  ]
}
```

Ny record:

```csharp
public sealed record ExportTranslationDto(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("name")] string Name);
```

`ExportCategoryDto` får:
```csharp
[property: JsonPropertyName("translations")]
IReadOnlyList<ExportTranslationDto>? Translations = null
```

**`programTagDefinitions`** — för v4 ersätts `IReadOnlyList<string>?` av ett nytt fält `programTagDefinitions` som istället innehåller objekt:

```json
"programTagDefinitions": [
  { "name": "Familjevänligt", "translations": [{ "locale": "en", "name": "Family Friendly" }] },
  { "name": "Rollspel" }
]
```

Ny record:

```csharp
public sealed record ExportProgramTagDefinitionDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("translations")]
    IReadOnlyList<ExportTranslationDto>? Translations = null);
```

`EditionExportDocument` ändras:

```csharp
// Gammalt (v1–v3):
[property: JsonPropertyName("programTagDefinitions")]
IReadOnlyList<string>? ProgramTagDefinitions = null

// Nytt (v4):
[property: JsonPropertyName("programTagDefinitions")]
IReadOnlyList<ExportProgramTagDefinitionDto>? ProgramTagDefinitions = null
```

**Bakåtkompatibel läsning av v3-export:** Eftersom JSON-deserialiseraren inte kan byta typ per schema-version behöver vi en custom `JsonConverter` eller ett intermediate-steg. Enklaste lösningen: ett separat fält `programTagStrings` (internt i deserialisering) som hanterar v1–v3, medan v4 läser det nya formatet.

Alternativt: importera v3-dokumentet som `JsonDocument`, kontrollera schema-version, deserializera till rätt record-typ. **Valt approach:** `ImportEditionCommand` tar emot ett `JsonDocument` och deserialiserar lokalt i handleren beroende på schema-version. *(Befintlig design — redan partiellt stödd.)*

**Export-logik (`EditionExportReadService`):**
- Hämta alla `CategoryTranslation` för upplagan i en query.
- Hämta alla `ProgramTagTranslation` för upplagan i en query.
- Bygg `ExportTranslationDto[]` per kategori / per tagg.
- Skriv med i exportdokumentet.

**Import-logik (`ImportEditionHandler`):**
- Efter att kategorier skapats: om `ExportCategoryDto.Translations != null`, kör `edition.SetCategoryTranslation(...)` per translation.
- Efter att program-tag-definitions skapats: om `ExportProgramTagDefinitionDto.Translations != null`, kör `edition.SetProgramTagTranslation(...)` per translation.

### 5. Admin-UI (utanför scope för /build)

Admin-gränssnittet för att redigera translations är inte i scope för denna ADR — det är ett framtida UI-arbete. API-endpoints skapas ändå så att admin-appen kan använda dem när UI byggs.

Endpoints:
- `PUT /api/editions/{editionId}/categories/{categoryId}/translations/{locale}` (admin)
- `DELETE /api/editions/{editionId}/categories/{categoryId}/translations/{locale}` (admin)
- `GET /api/editions/{editionId}/categories/{categoryId}/translations` (admin, lista alla)
- `PUT /api/editions/{editionId}/tag-translations/{tagName}/{locale}` (admin)
- `DELETE /api/editions/{editionId}/tag-translations/{tagName}/{locale}` (admin)
- `GET /api/editions/{editionId}/tag-translations` (admin, lista alla)

---

## Motivering

**Varför child-entitet på `Category` och inte eget aggregat?**  
`CategoryTranslation` saknar livscykel utanför `Category`. Kategorier skapas/uppdateras av admin — inga registreringsflöden involverade. Äga translation i aggregathierarkin Edition → Category → CategoryTranslation är konsistent med hur `PageTranslation` och `EventTranslation` är modellerade.

**Varför naturlig nyckel `(EditionId, TagName, Locale)` för `ProgramTagTranslation` och inte ett surrogate ID?**  
`ProgramTagDefinition` är ett value object identifierat av sitt namn. Att inte ändra detta (inga migrationer av befintlig tabell, inget aggregat-refaktorering) väger tyngre än estetiken i att ha ett GUID. Den naturliga nyckeln är stabil — taggar byter inte namn utan att tas bort och återskapas.

**Varför C#-sida fallback och inte SQL COALESCE?**  
De flesta queries hämtar alla events per upplaga — inte enskilda events. En bulk-fetch av translations per `EditionId` följt av C#-lookup undviker komplex SQL och är lättare att testa. Datamängden (typiskt < 1000 events, < 50 kategorier, < 100 taggar per upplaga) motiverar inte SQL-optimering.

**Varför schema version 4 och inte bakåtkompatibelt tillägg till v3?**  
`programTagDefinitions` ändrar typ från `string[]` till objekt-array. JSON-deserialisering kan inte hantera det utan typskillnad. En ren schema-version-höjning är tydligare än en hybrid-lösning med nullable fält.

---

## Bounded contexts och filer som påverkas

| Vad | Fil |
|---|---|
| `CategoryTranslation` entitet | `Domain/Convention/Entities/CategoryTranslation.cs` (ny) |
| `Category.UpsertTranslation()` | `Domain/Convention/Entities/Category.cs` |
| `Edition.SetCategoryTranslation()` | `Domain/Convention/Aggregates/Edition.cs` |
| `Edition.SetProgramTagTranslation()` | `Domain/Convention/Aggregates/Edition.cs` |
| `ProgramTagTranslation` entitet | `Domain/Convention/Entities/ProgramTagTranslation.cs` (ny) |
| EF Core-konfiguration CategoryTranslation | `Infrastructure/Persistence/Configurations/Convention/CategoryTranslationConfiguration.cs` (ny) |
| EF Core-konfiguration ProgramTagTranslation | `Infrastructure/Persistence/Configurations/Convention/ProgramTagTranslationConfiguration.cs` (ny) |
| Migration | `AddCategoryAndTagTranslations` |
| `SetCategoryTranslationCommand` + handler | `Application/Convention/Commands/SetCategoryTranslation/` (ny) |
| `SetProgramTagTranslationCommand` + handler | `Application/Convention/Commands/SetProgramTagTranslation/` (ny) |
| `IEditionRepository` | utökas med `GetCategoryTranslationsAsync` och `GetTagTranslationsAsync` |
| `EditionRepository` | implementation av nya metoder |
| `IEventRepository` / read service | locale-param på publika queries |
| `EditionExportReadService` | hämtar och inkluderar translations |
| `ImportEditionHandler` | sätter translations vid import |
| `EditionExportDocument` | schema v4, ny DTO-typer |
| Endpoints | EditionEndpoints.cs + ny route-grupp |
| Frontend `EventService` | skickar locale-param på publika fetch |

---

## Risker

| Risk | Sannolikhet | Åtgärd |
|---|---|---|
| `ProgramTagTranslation`-tabellens naturliga nyckel on varchar(64) kan ge prestandaproblem | Låg | Index på edition_id täcker lookup; 64 tecken är standard för tag-kolumner |
| v3-export kan inte deserialiseras till v4-struktur | Hög | Import-hanteraren kontrollerar schema-version och väljer deserialiseringsväg |
| Translations laddas in även när `locale = null` (onödig DB-roundtrip) | Låg | Villkorsstyrd laddning: ladda bara translations när `locale != null` |
| Admin redigerar tags utan att uppdatera translations (stale translations) | Medel | `ProgramTagTranslation` tas bort kaskadvis om taggen tas bort (ON DELETE CASCADE) |

---

## Acceptanskriterier

### R-I18N06 — Kategori-översättningar

- [x] `Category.UpsertTranslation(locale, name)` skapar/uppdaterar `CategoryTranslation`
- [x] `Edition.SetCategoryTranslation(categoryId, locale, name)` delegerar korrekt
- [x] Okänd locale ger `ArgumentException`
- [x] Okänt `categoryId` ger `CategoryNotFoundInEditionException` i handleren
- [x] `GET /api/public/editions/{id}/events?locale=en` returnerar engelska kategorinamn för events med translation
- [x] `GET /api/public/editions/{id}/events?locale=en` returnerar originalnamnet tyst om translation saknas
- [x] Export v4 inkluderar `translations`-array i `categories`
- [x] Import v4 sätter kategori-translations efter kategoriskapande
- [x] Import v3 importeras utan fel (translations ignoreras — saknas i formatet)
- [x] Enhetstest: `Category.UpsertTranslation()` — lyckligt flöde, skapar ny, uppdaterar befintlig, felfall
- [x] Handlertester: `SetCategoryTranslationCommand` — lyckligt flöde och felfall

### R-I18N07 — Programtags-översättningar

- [x] `Edition.SetProgramTagTranslation(tagName, locale, translatedName)` skapar/uppdaterar `ProgramTagTranslation`
- [x] Okänt taggnamn ger `ProgramTagDefinitionNotFoundException`
- [x] Okänd locale ger `ArgumentException`
- [x] `GET /api/public/editions/{id}/events?locale=en` returnerar engelska taggnamn
- [x] Tyst fallback till originalnamnet om translation saknas
- [x] Export v4: `programTagDetails` är array av objekt med valfri `translations`-array; `programTagDefinitions` behålls som `string[]` för bakåtkompatibilitet
- [x] Import v4 sätter tagg-translations efter tag-definitions skapande
- [x] Import v3 (string-format) importeras utan fel
- [x] Enhetstest: `Edition.SetProgramTagTranslation()` — lyckligt flöde, felfall
- [x] Handlertester: `SetProgramTagTranslationCommand`
