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

**SCSS per komponent** – stilar är komponentlokala. Inga globala utility-klasser.
Inga CSS-ramverk (Tailwind etc.).

**Responsivitet** – admin-appen är desktop-first. Formulär använder
`flex-wrap` för att tåla smalare fönster, men mobil är inte ett krav.

---

### Typer och modeller

Alla DTO-typer definieras i `shared`-biblioteket under
`projects/shared/src/lib/models/convention.models.ts`.

Komponenter importerar typer från `'shared'` – aldrig inline-typer för
API-data.

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
> som kan justeras när implementation börjar.

### Syfte och målgrupp
Besökar-/arrangörs-/staffsida riktad mot konventionens deltagare. Stylas
enligt konventionens profil, inte ett generellt admin-UI. Mobilanpassning
är ett primärkrav.

### Planerade avvikelser från admin-appen

| Område | Admin | Publik |
|--------|-------|--------|
| Tema | Angular Material standard | Konventionsbrandad (anpassad palette) |
| Responsivitet | Desktop-first | Mobile-first |
| Auth | Alltid inloggad (admin) | Blandat – läsvyer är publika, formulär kräver inloggning |
| Formulär | Inline på sidan | Möjligen egna rout-baserade formulär-sidor |
| State | Signals + lokal state | Samma: Signals, ingen NgRx |
| Laddningstillstånd | Spinner | Skeleton-loading (bättre UX för publik) |
| Fel | Inline felmeddelande | Troligen inline, men mer genomtänkt UX |

### Autentisering
- Publika GET-endpoints kräver ingen token
- Registrerings- och ansökningsflöden kräver inloggning (samma JWT-mekanism)
- Ingen `adminGuard` – guard baseras på autentisering, ej admin-roll

### Strukturplan
```
features/
  program/         – evenemangsschema (publik, ingen auth)
  register/        – besökarregistrering (auth krävs)
  apply-staff/     – staffansökan (auth krävs)
  submit-event/    – arrangörsflöde (auth krävs)
  my-registrations/– mina registreringar
```
