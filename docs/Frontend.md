# Frontend-arkitektur

Dokumentet beskriver de principer, mönster och konventioner som gäller för
frontend-implementationen. Admin-appen och den publika appen hålls separata och
kan skilja sig åt.

---

## Admin-appen (`projects/admin`)

### Syfte och målgrupp
Rollbaserad app för konventionsadministratörer. Kräver inloggning och
`is_admin`-claim för åtkomst. Fokus på dataintensiva formulär, listor och
arbetsflöden – inte visuell konventionsprofil.

---

### Komponentarkitektur

**Standalone-komponenter** – varje komponent deklarerar sina egna importer.
Inga NgModules.

**En komponent per route** – varje sida är sin egen lazy-loadad komponent.
Inga "page wrapper"-komponenter som wrappar en annan komponent.

**Inga delade UI-komponenter** såvida inte exakt samma markup används på minst
tre ställen och det finns ett tydligt värde av abstraktion. Preferens: upprepa
hellre tre liknande `mat-card`-block än att skapa en generisk wrapper.

**Filstruktur per feature:**
```
features/
  editions/
    edition-detail/
      edition-detail.component.ts
      edition-detail.component.html
      edition-detail.component.scss
  persons/
    persons.component.ts
    ...
```

---

### Tillståndshantering

**Angular Signals** – all lokal komponentstate hanteras med `signal()` och
`computed()`. Inga Observables i templates, ingen `async`-pipe, ingen NgRx.

**Standardsignaler per komponent som laddar data:**
```typescript
readonly loading = signal(true);
readonly error   = signal<string | null>(null);
readonly saving  = signal(false);
readonly data    = signal<FooDto | null>(null);
```

- `loading` – spinner visas tills initial data är hämtad
- `saving`  – knappar disabled under pågående mutation
- `error`   – felmeddelande visas i komponenten, ej toast/dialog

**Ingen delad state** mellan komponenter. Data laddas om vid navigation.
`reload()` – privat metod som hämtar om data efter en mutation.

---

### Datahämtning

Initialhämtning sker i `ngOnInit`. Komponenten implementerar `OnInit`.

```typescript
ngOnInit(): void {
  const id = this.route.snapshot.paramMap.get('id')!;
  this.svc.getEdition(id).subscribe({
    next: e => { this.edition.set(e); this.loading.set(false); },
    error: () => { this.error.set('Kunde inte hämta upplagedata.'); this.loading.set(false); },
  });
}
```

Parallella anrop (t.ex. edition + persons) startas i samma `ngOnInit` utan att
vänta på varandra.

---

### Formulär

**Reactive Forms** via `FormBuilder` för alla create/edit-formulär.

Formulär deklareras som `readonly`-fält direkt på komponenten:
```typescript
readonly venueForm = this.fb.group({
  name:        ['', Validators.required],
  building:    ['', Validators.required],
  description: [''],
});
```

**Inline-formulär** – formulär visas direkt på sidan under den lista de
tillhör, inte i dialog/modal. Formuläret återställs (`reset()`) vid lyckat
submit.

**Submit-metod:**
1. Validera (`if (form.invalid) return`)
2. Sätt `saving.set(true)`
3. Anropa service
4. `next`: `reload()`, `form.reset()`, `saving.set(false)`
5. `error`: `handleError(kontext, err)`

---

### Listningssidor

Standardmönstret för en listningssida i admin-appen. Avvikelser kräver
motivering. Se `persons.component` som referensimplementation.

#### Struktur

```
page-header          – rubrik + undertitel
action-bar           – sökfält (vänster) + primärknapp (höger)
[create-card]        – kollapsbar mat-card med skapaformulär (visas vid behov)
mat-card > data-table – listning av entiteter
```

#### Komponentsignaler

Utöver standardsignalerna `loading`, `error`, `saving` tillkommer:

```typescript
readonly items        = signal<FooDto[]>([]);
readonly searchQuery  = signal('');
readonly showCreateForm = signal(false);
readonly editingItem  = signal<FooDto | null>(null);

readonly filteredItems = computed(() => {
  const q = this.searchQuery().toLowerCase();
  return this.items().filter(i => !q || /* matchning på relevanta fält */);
});
```

#### Sökning

Klientsidessökning med `computed()` på redan laddad lista. Sökning sker på
namn och e-post (eller motsvarande identifierande fält). Ingen debounce –
direkt filtrering räcker för admin-listor.

#### Skapa-formulär

Dolt som default, visas via toggle-knapp. `mat-card` med `form-row`-grid
(flex, `flex-wrap`). Återställs och stängs vid lyckat submit.

#### Tabellen

`<table class="data-table">` direkt inuti `<mat-card-content class="no-pad">`.
Kolumner: identifierande fält (name, email etc.) → statuskolumn → tom
actions-kolumn med `class="actions-col"`.

Åtgärdsknappar samlas i `<td class="row-actions">` med `mat-icon-button`.
Typiska åtgärder: edit (alltid) + kontextuell knapp (ta bort / avaktivera /
återaktivera beroende på entitetens tillstånd).

#### Inline-redigering

Redigeringsformuläret visas som en extra `<tr class="edit-row">` direkt under
den rad som redigeras. Raden visas via `editingItem()?.id === item.id`.
Spara/avbryt-knappar ersätter de vanliga action-knapparna i samma rad (ej
extra knappar utanför tabellen).

```html
@if (editingItem()?.id === item.id) {
  <tr class="edit-row">
    <td colspan="N">
      <form [formGroup]="editForm" class="form-row"> … </form>
    </td>
  </tr>
}
```

#### Tom lista

```html
@empty {
  <tr><td colspan="N" class="empty-cell">Inga X tillagda ännu.</td></tr>
}
```

---

### Felhantering

Privat `handleError`-metod extraherar `ProblemDetails.detail` från API-svaret:

```typescript
private handleError(context: string, err: unknown): void {
  const detail = (err as { error?: { detail?: string } })?.error?.detail;
  this.error.set(detail ? `${context}: ${detail}` : context);
  this.saving.set(false);
}
```

Felmeddelanden är på svenska och anger kontext ("Kunde inte skapa lokal: …").

---

### Tjänstelager

**`ConventionService`** i `shared`-biblioteket hanterar all HTTP mot API:t.
Komponenter injicerar enbart `ConventionService` – aldrig `HttpClient` direkt.

Ny API-operation → ny metod i `ConventionService`. Request-typer definieras
som interface i samma fil.

Convention-ID och auth-header sätts automatiskt av interceptors i `shared`.

---

### Routing

- Alla routes under shell-layouten skyddas av `authGuard` + `adminGuard`
- Lazy-loading via `loadComponent` för varje feature-sida
- Navigering med `router.navigate([...])` eller `routerLink`
- Parametrar läses med `route.snapshot.paramMap` (ej Observable-baserat)

---

### UI-bibliotek och visuellt

**Angular Material** – standardkomponenter för alla UI-element. Inga egna
design-tokens eller override-teman utan explicit beslut.

Använda komponenter:
`mat-card`, `mat-button`, `mat-icon`, `mat-form-field`, `mat-input`,
`mat-select`, `mat-chip`, `mat-expansion-panel`, `mat-progress-spinner`,
`mat-tooltip`, `mat-sidenav`

**SCSS per komponent** – stilar är i första hand komponentlokala. Undantaget
är de globala utility-klasserna i `styles.scss` (se nedan).
Inga CSS-ramverk (Tailwind etc.).

**Globala utility-klasser** (definierade i `styles.scss`, används fritt i alla
komponenter):

| Klass | Syfte |
|-------|-------|
| `.data-table` | Standardtabell för listningar (se Listningssidor) |
| `.row-actions` | Flex-container för edit/delete-knappar i tabellrad |
| `.chip`, `.chip-green`, `.chip-grey`, `.chip-blue` | Statuspiller |

**Responsivitet** – admin-appen är desktop-first. Formulär använder
`flex-wrap` för att tåla smalare fönster, men mobil är inte ett krav.

---

### Typer och modeller

Alla DTO-typer definieras i `shared`-biblioteket under
`projects/shared/src/lib/models/convention.models.ts`.

Komponenter importerar typer från `'shared'` – aldrig inline-typer för
API-data.

---

### Etiketter och texter – ingen hårdkodning

**Regel: inga svenska (eller andra naturliga språk) texter får vara hårdkodade direkt i `.html`- eller `.ts`-filer.**

All text som visas för användaren definieras i en dedikerad labelkälla och importeras därifrån. Det gäller utan undantag:

- Navigationsrubriker och menyetiketter
- Sidrubriker, undertexter
- Knappar, tooltips, aria-labels
- Formulärfältetiketter (`mat-label`) och platshållare (`placeholder`)
- Felmeddelanden (både inline i template och i `.ts`-filer)
- Tomma-lista-meddelanden (`empty-cell`-text)
- Statusetiketter och chip-text
- Bekräftelsetexter och dialogrubriker
- Tabellkolumnrubriker

#### Var labels definieras

| Typ | Plats |
|-----|-------|
| Domänstatusardar (EventStatus, StaffApplicationStatus m.fl.) | `projects/shared/src/lib/labels/*.labels.ts` – exporteras via `public-api.ts` |
| Gemensamma UI-åtgärder (Spara, Avbryt, Redigera, Ta bort…) | `projects/admin/src/app/labels/ui.labels.ts` |
| Navigationsrubriker | `projects/admin/src/app/labels/nav.labels.ts` |
| Felmeddelanden per domän | `projects/admin/src/app/labels/errors.labels.ts` |
| Sidspecifika texter (rubriker, empty states) | `projects/admin/src/app/labels/pages.labels.ts` |

Filer i `shared/lib/labels/` exporteras via `public-api.ts` och är tillgängliga i alla appar.
Filer under `admin/labels/` är admin-appens egna och importeras direkt av komponenterna.

#### Format

Label-filer exporterar namngivna `Record<string, string>`-konstanter eller enkla objekt:

```typescript
// ui.labels.ts
export const ACTION = {
  save:   'Spara',
  cancel: 'Avbryt',
  create: 'Skapa',
  delete: 'Ta bort',
  edit:   'Redigera',
} as const;
```

Komponenter importerar och exponerar etiketten som en `readonly`-property:

```typescript
readonly ACTION = ACTION;   // tillgängliggör i template
```

I template: `{{ ACTION.cancel }}` eller `[matTooltip]="ACTION.edit"`.

#### Framtida i18n

Strukturen är förberedd för flerspråksstöd. När behov uppstår introduceras en
`LabelsService` som väljer rätt lokalisering vid runtime – komponenterna behöver
då bara byta från direktimport till service-injektion, utan att ändra templates.

---

### Konventioner

- `inject()` för dependency injection, aldrig konstruktor-injektion
- `readonly` på alla injekterade tjänster och signaler
- Metoder för user actions: `publish()`, `addVenue()`, `openRegistration(type)` – inte `onPublish`, `handleAddVenue`
- Template: `@if`, `@for`, `@else` (Angular 17+ control flow) – inga `*ngIf`/`*ngFor`
- Ingen `as` i template-bindings utom `@else if (data(); as x)` för null-koalescering

---

## Publika appen (`projects/public`)

> Appen är ännu inte påbörjad. Principerna nedan är avsiktsförklaringar
> som kan justeras när implementation börjar. Se `docs/public-mockup.html`
> för interaktiv skissbild av alla skärmar.

### Syfte och målgrupp
Besökar-/arrangörs-/staffsida riktad mot konventionens deltagare. Stylas
enligt konventionens profil, inte ett generellt admin-UI. Mobilanpassning
är ett primärkrav.

---

### Routing

```
/                                    → HemComponent                (publik)
/program                             → ProgramComponent             (publik)
/program/:id                         → EventDetailComponent         (publik)
/login                               → LoginComponent               (publik)
/mina-sidor                          → MinaSidorComponent           (authGuard)
/mina-sidor/biljett                  → MinBiljettComponent          (authGuard)
/mina-sidor/program                  → MittProgramComponent         (authGuard)
/mina-sidor/arrangemang              → ArrangemangListComponent     (authGuard)
/mina-sidor/arrangemang/nytt         → ArrangemangFormComponent     (authGuard)
/mina-sidor/arrangemang/:id          → ArrangemangDetailComponent   (authGuard)
/mina-sidor/bemanning                → MinBemanningComponent        (authGuard)
```

---

### Shell och navigation

`ShellComponent` med `mat-toolbar` som app-topnav (konventionsbrandad).
Inga sidomenyer – allt navigeras via topnav och `routerLink`.

- Publika routes (`/`, `/program`, `/program/:id`) är tillgängliga utan token.
- `authGuard` (från shared) skyddar alla `/mina-sidor/**`-routes och
  redirectar till `/login` om inget token finns.
- Ingen `adminGuard` används i publika appen.

**Auth-tillstånd i topnav:**
- Ej inloggad: visar "Logga in"-knapp
- Inloggad: visar "Mina sidor"-länk + användarnamn-chip med dropdown
- Använder `AuthService.isAuthenticated` (signal) från shared-biblioteket

---

### Responsivitet och Mobile First

Den publika appen är **mobile first**: grundstilarna gäller för den minsta
skärmen (320 px) och utökas uppåt med `@media (min-width: …)`.
Skriv aldrig `@media (max-width: …)` – om det behövs är grundstilen fel.

#### Brytpunkter

| Namn | Bredd | Typisk enhet |
|------|-------|--------------|
| sm   | 480px | Stor telefon, liggande |
| md   | 768px | Platta, liten desktop |
| lg   | 1024px | Desktop |
| xl   | 1200px | Bred skärm (max content-bredd) |

```scss
// ✓ Mobile first
.element { font-size: 1.5rem; }
@media (min-width: 768px) { .element { font-size: 2.5rem; } }

// ✗ Desktop first – undvik
.element { font-size: 2.5rem; }
@media (max-width: 767px) { .element { font-size: 1.5rem; } }
```

#### Navigation – hamburgermeny

Under `md` kollapsar topnav till hamburgermeny:

- `.nav-links` och `.nav-actions` har `display: none` som default
- En hamburgersknapp (`mat-icon-button`, ikon `menu`/`close`) visas med `margin-left: auto`
- En `.mobile-menu` (dropdown direkt under topnav) öppnas via `menuOpen = signal(false)`
  i `ShellComponent`
- Menyn stängs när en länk klickas (`(click)="menuOpen.set(false)"`)
- På `md+` är hamburgern `display: none` och nav-links/actions `display: flex`

#### Touchmål

Alla interaktiva element har minst **44 × 44 px** klickbar yta.
Dag-tabs och kategori-chips: `min-height: 44px; padding: 10px 16px`.

#### Grid-beteende

| Element | Mobil (default) | md (768px+) | lg (1024px+) |
|---------|-----------------|-------------|--------------|
| CTA-grid (hem) | 1 kolumn | 2 kolumner | 3 kolumner (auto-fit) |
| Event-grid (hem, program) | 1 kolumn | 2 kolumner | 3 kolumner (auto-fill) |
| Event-detalj | Enkolumns (sidebar sist) | Tvåkolumns (1fr 280px) | — |

#### Typografiskala

| Element | Mobil | md+ |
|---------|-------|-----|
| Hero-titel | 2rem | 3rem |
| Hero padding | 48px top / 60px bottom | 80px top / 100px bottom |
| Evenemangstitel (detalj) | 1.5rem | 2rem |

#### Horisontell scroll för filter

Dag-tabs och kategori-chips: `overflow-x: auto; flex-wrap: nowrap` på mobil.
Scrollbaren döljs (`scrollbar-width: none`). På `md+`: `flex-wrap: wrap`.

---

### Mina sidor – navigationsstruktur

"Mina sidor" är en rollindelad yta. En person kan vara besökare, arrangör
och funktionär simultant. Navigationen är alltid synlig med alla sektioner;
varje sektion hanterar sitt eget tomma state med en tydlig CTA.

```
/mina-sidor
  Min biljett              ← rollneutral (alla behöver en biljett)
  ─ Som besökare ──────
    Mitt program           ← sessioner man anmält sig till
  ─ Som arrangör ──────
    Mina arrangemang       ← lista + skapa/redigera
  ─ Som funktionär ─────
    Min bemanning          ← ansökan + tilldelade pass
```

**Rolldetektering** – den publika appen har inga formella roller i JWT-meningen.
Varje sektionskomponent hämtar sin egen data och visar CTA om svaret är tomt:

| Sektion | Backend-källa |
|---------|---------------|
| Min biljett | `GET /editions/{id}/my-visitor-registration` |
| Mitt program | `GET /editions/{id}/my-session-registrations` |
| Mina arrangemang | `GET /editions/{id}/my-events` |
| Min bemanning | `GET /editions/{id}/my-staff-application` |

`MinaSidorComponent` (hub) laddar alla fyra parallellt och visar ett
kompakt statuskort per sektion med länk till respektive under-route.

---

### Skeleton loading

Använd CSS skeleton shimmer i stället för `mat-spinner`. Definieras som
global utility-klass i `styles.scss`:

```scss
.skeleton {
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 4px;
}
@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

---

### Konventionsbranding

Temat konfigureras via CSS custom properties i `styles.scss`:

```scss
:root {
  --brand-primary: #{$brand-primary};  // t.ex. #1b2a4a
  --brand-accent:  #{$brand-accent};   // t.ex. #e8920a
}
```

Angular Material custom theme bygger på dessa variabler. Värden sätts per
deploy via `environment.ts` → `environment.brandPrimary` /
`environment.brandAccent`.

---

### Edition-kontext

Den publika appen visar alltid den aktiva/publicerade upplagan. Inget
`EditionContextService` med val bland upplagor (till skillnad från admin).

**EditionService** (singleton, `providedIn: 'root'`):
- Laddar aktiv upplaga vid app-start via `APP_INITIALIZER`
- Exponerar `editionId` som signal
- Andra services läser `editionService.editionId()` i stället för att
  skicka ID som parameter

---

### Publika API-anrop (utan auth)

Feed-endpointarna används för offentligt innehåll:

| Endpoint | Används av |
|----------|-----------|
| `GET /feed/editions/{id}` | Landningssida, program-lista |
| `GET /feed/events/{id}` | Evenemangsdetalj |

Dessa returnerar enbart publicerade data och kräver inget token.

---

### Komponentmönster

Samma standalone-komponentmönster som admin. Avvikelser:

| Aspekt | Admin | Publik |
|--------|-------|--------|
| Layout | Sidenav + sidebar | Top nav, full-width content |
| Laddning | `mat-spinner` | Skeleton shimmer |
| Formulär | Inline på sidan | Egna route-baserade formulärsidor |
| Auth | Alltid inloggad | Blandat – publik/skyddad |
| Responsivitet | Desktop-first | Mobile-first |
| Tema | Material standard | Konventionsbrandad via CSS-variabler |
| State | Signals + lokal | Samma + `EditionService` singleton |

**Formulärflöde** – skickade formulär navigerar med `router.navigate`
snarare än inline-reset. Bekräftelse visas som separat vy eller alert.

**Mobil-first CSS** – `max-width`-containers, `flex-direction: column` på
smala skärmar, generösa touch-targets (min 44px höjd på knappar).

---

### Strukturplan

```
features/
  hem/               – landningssida (publik)
  program/           – evenemangslista + filtrering (publik)
  event-detail/      – evenemangsdetalj + sessioner (publik)
  auth/              – login-formulär (publik)
  mina-sidor/
    hub/             – MinaSidorComponent: alla tre rollsektioner parallellt
    besökarregistrering/ – VisitorRegistrationComponent (authGuard)
    evenemang/
      submit/        – SubmitEventComponent (authGuard)
      detail/        – MyEventComponent (authGuard)
    staff/           – StaffApplicationComponent (authGuard)
```
