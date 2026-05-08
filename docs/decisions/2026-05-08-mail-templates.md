# ADR: Adminredigerbara mailmallar (UC-RC005, UC-RC006)

## Kontext

Rich Content-arbetet (R-RC01–R-RC03) är genomfört: markdown-redigering, bilduppladdning och informationssidor fungerar. Det som återstår är R-RC04: adminredigerbara mailmallar.

Idag är alla e-postmallar hårdkodade som statiska metoder i `EmailTemplates.cs` (infrastrukturlagret). Det finns 11–13 distinkta mailtyper. Outbox-mönstret fungerar väl: `IEmailService` → `OutboxEmailService` → `outbox_messages`-tabell → `OutboxProcessor` → `IDirectEmailSender`. Ingen mallmotor, ingen mall-tabell i databasen.

**Problemet:** Konventionsadministratörer kan inte anpassa e-postkommunikationen utan en redeploy. Text, ton och signaturer är inlåsta i källkoden.

## Beslut

Vi implementerar adminredigerbara mailmallar med följande design:

### Scope: vilka mailtyper är anpassningsbara?

Konventionsspecifika mailtyper (där innehållet beror på konventionens ton och profil) anpassas av admin. Identitets- och systemmail hålls utanför – de rör säkerhetskritiska flöden och ska inte kunna brytas av en mallredigering.

**Anpassningsbara:**
| MailTemplateType | Variabler |
|---|---|
| `VisitorRegistrationConfirmed` | `{{firstName}}`, `{{conventionName}}` |
| `StaffApplicationReceived` | `{{firstName}}`, `{{conventionName}}` |
| `StaffApplicationAccepted` | `{{firstName}}`, `{{conventionName}}` |
| `StaffApplicationRejected` | `{{firstName}}`, `{{conventionName}}` |
| `EventApproved` | `{{firstName}}`, `{{eventTitle}}`, `{{conventionName}}` |
| `EventRejected` | `{{firstName}}`, `{{eventTitle}}`, `{{rejectionComment}}`, `{{conventionName}}` |
| `CoOrganiserInvitation` | `{{firstName}}`, `{{eventTitle}}`, `{{inviteLink}}` |

**Ej anpassningsbara (hårdkodade som idag):**
- `PasswordReset`, `EmailConfirmation`, `ResendConfirmation`, `PasswordChanged`
- `TenantSignupWelcome`, `TenantProvisionedWelcome`

### Domänmodell

`MailTemplate` placeras i `Content`-bounded context (samma som `Page`). Det är ett aggregat-rot med:

```csharp
public sealed class MailTemplate : AggregateRoot
{
    public MailTemplateId Id { get; private set; }
    public ConventionId ConventionId { get; private set; }
    public MailTemplateType TemplateType { get; private set; }  // enum
    public string Subject { get; private set; }  // max 500 tecken, variabelplatshållare tillåtna
    public string BodyMarkdown { get; private set; }  // max 20 000 tecken, variabelplatshållare tillåtna
    public bool IsCustomized { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Customize(string subject, string bodyMarkdown);  // sätter IsCustomized = true
    public void ResetToDefault(string defaultSubject, string defaultBodyMarkdown);  // sätter IsCustomized = false
}
```

Templates är **Convention-scopade**, inte Edition-scopade. Mailtyper som StaffApplication och CoOrganiserInvitation spänner över hela konventionens livscykel.

### Variabelsubstitution

Enkel regex-baserad ersättning: `{{variabelnamn}}` → värde. Implementeras i en ny `IMailTemplateRenderer`-service i applikationslagret:

```csharp
public interface IMailTemplateRenderer
{
    string RenderSubject(string template, IReadOnlyDictionary<string, string> variables);
    string RenderBody(string markdownTemplate, IReadOnlyDictionary<string, string> variables);
}
```

Okänt variabelnamn → tom sträng (inget undantag). Implementationen gör `Regex.Replace` med en `MatchEvaluator`.

Markdig används för markdown→HTML-rendering av `BodyMarkdown` innan det läggs i outboxen. Markdig är redan ett transitivt beroende via `ngx-markdown` på frontend; på backend läggs `Markdig`-paketet till i Infrastructure-projektet.

### Integrering med outbox

`OutboxEmailService` injicerar `IMailTemplateRenderer` och en `IMailTemplateRepository`. Vid anrop som skickar ett anpassningsbart mail:

1. Hämta `MailTemplate` för aktuell `ConventionId` + `TemplateType` (om den finns)
2. Finns ingen → använd `DefaultMailTemplates.GetTemplate(type)` (statisk klass i Application-lagret)
3. Rendera subject och body med variabel-dict
4. Rendera markdown → HTML med Markdig
5. Lägg `EmailPayload` i outboxen som vanligt

`IEmailService`-metoderna behöver utökas med `ConventionId` som parameter för anpassningsbara typer.

### Databasschema

Ny tabell `mail_templates` i `dbo`-schemat:

| Kolumn | Typ | Regler |
|---|---|---|
| `id` | `uniqueidentifier` | PK |
| `convention_id` | `uniqueidentifier` | FK → `conventions.id`, NOT NULL |
| `template_type` | `nvarchar(100)` | NOT NULL |
| `subject` | `nvarchar(500)` | NOT NULL |
| `body_markdown` | `nvarchar(max)` | NOT NULL |
| `is_customized` | `bit` | NOT NULL |
| `updated_at` | `datetimeoffset` | NOT NULL |

Unikt index: `(convention_id, template_type)`.

En `MailTemplate`-rad skapas **inte** automatiskt vid convention-skapande. I stället hämtar systemet mallen "lazy": om ingen rad finns för en given `(conventionId, templateType)` används standardmallen. Raden skapas eller uppdateras när admin sparar för första gången (`Upsert`-semantik i handlern).

### API-endpoints

```
GET  /api/conventions/{id}/mail-templates         → ListMailTemplates (admin)
GET  /api/conventions/{id}/mail-templates/{type}  → GetMailTemplate (admin)
PUT  /api/conventions/{id}/mail-templates/{type}  → UpdateMailTemplate (admin)
POST /api/conventions/{id}/mail-templates/{type}/reset → ResetMailTemplate (admin)
```

`GET`-anropen returnerar alltid ett svar – antingen den anpassade mallen eller standardmallens text (merged DTO). Klienten ser aldrig "null" – alltid en redigerbar text.

### Admin-UI

Ny sida i admin-appen: `Inställningar → E-postmallar`. Lista alla anpassningsbara malltyper med status (standard/anpassad). Klicka en typ → redigeringsvy med:
- Ämnesrad (textfält med variabelhjälp)
- Brödtext (markdown-editor, samma komponent som för informationssidor)
- Live-preview med exempelvariabler
- Knapp "Återställ till standard" (aktiv när `isCustomized = true`)

## Motivering

**Varför Convention-scoped och inte Edition-scoped?**  
Mailtyper som "Personnärsansökan mottagen" och "Inbjudan som medarrangör" är inte bundna till en upplaga. Att kräva omskapning per upplaga ger onödigt merarbete.

**Varför enkel regex och inte Liquid/Handlebars?**  
Variabeluppsättningen är statisk och väldefinierad per malltyp. En template engine tillför komplexitet (XSS-risk om admin kan skriva `{% raw %}{% if %}{% endraw %}`-logik, beroende att hantera) utan att lösa ett verkligt problem. Regex-replace är deterministisk, testbar och räcker.

**Varför Markdig och inte en annan markdown-renderer?**  
Markdig är en snabb, standardkonform .NET-implementation. DOMPurify-sanering sker på frontend; på backend renderar vi markdown till HTML för utskick (e-post är HTML). Markdig-paketet (`Markdig`) är välunderhållet och saknar stora transitivt beroende.

**Varför inte skapa mall-rader automatiskt vid convention-skapande?**  
Det undviker en migration som behöver backfilla rader för befintliga konventioner. Lazy-hämtning är enklare och ger samma beteende.

## Bounded contexts som påverkas

| BC | Aggregat/handler/tabell | Förändring |
|---|---|---|
| Content | `MailTemplate` (ny aggregate root) | Ny tabell `mail_templates`, CRUD-handlers |
| Infrastructure/Email | `OutboxEmailService`, `IEmailService` | Injicerar IMailTemplateRenderer, ConventionId parameter |
| Convention | `Convention` (ingen ändring) | Används som scope-referens |
| API | `MailTemplateEndpoints` (ny) | 5 endpoints |
| Admin-frontend | `mail-templates/` feature (ny) | Lista + redigeringsvy |

## Risker

- **Trasig variabelreferens:** Admin skriver `{{fistName}}` (stavfel). Visas som tom sträng – inga undantag, men felaktigt innehåll. Mildras med variabelhjälp-panel i UI.
- **Markdig XSS i utskick:** Renderat HTML läggs direkt i mail-body. Mildras med att vi kör `Markdig` i safe mode (`DisableHtml`-alternativet förhindrar råa HTML-taggar) eftersom vi inte behöver dem i e-post.
- **IEmailService-signaturer:** Sju metoder behöver `ConventionId`-parameter. Befintliga anropare (domain event handlers) måste uppdateras. Riskerar att missa ett anropsställe. Mitigering: kompilatorfel om signaturen ändras – inga runtime-risker.

## Implementationsordning

1. **Domänlager** – `MailTemplate`, `MailTemplateType` enum, `MailTemplateId`
2. **Applikationslager** – `IMailTemplateRenderer`, `DefaultMailTemplates`, `IMailTemplateRepository`, commands (`UpdateMailTemplate`, `ResetMailTemplate`), queries (`GetMailTemplate`, `ListMailTemplates`)
3. **Infrastruktur** – `MailTemplateRepository`, `MarkdigMailTemplateRenderer`, EF Core-konfiguration, migration, uppdaterade `IEmailService`-signaturer och `OutboxEmailService`
4. **API** – `MailTemplateEndpoints`
5. **Frontend** – `mail-templates`-feature i admin-appen

## Acceptanskriterier

- [ ] `MailTemplate`-aggregat finns med metoderna `Customize()` och `ResetToDefault()`
- [ ] `MailTemplateType`-enum täcker de 7 anpassningsbara typerna
- [ ] `IMailTemplateRenderer` ersätter variabelplatshållare; okänd variabel → tom sträng
- [ ] `DefaultMailTemplates` innehåller fungerande standardtexter för alla 7 typer
- [ ] Markdig renderar markdown till HTML med `DisableHtml` (ingen råHTML i mallar)
- [ ] `PUT /api/conventions/{id}/mail-templates/{type}` sparar anpassad mall, sätter `IsCustomized = true`
- [ ] `POST /api/conventions/{id}/mail-templates/{type}/reset` sätter `IsCustomized = false`
- [ ] `GET`-endpoints returnerar alltid ett svar (anpassad text eller standardtext)
- [ ] Anpassad mall används vid nästa utskick av rätt typ och konvention
- [ ] Admin-UI listar alla 7 malltyper med status (standard/anpassad)
- [ ] Redigeringsvy har markdown-editor, variabelhjälp och "Återställ"-knapp
- [ ] Enhetstester för `MailTemplate`-domänmetoderna
- [ ] Enhetstester för `IMailTemplateRenderer`-implementationen (regex, okänd variabel)
- [ ] Handlertester för `UpdateMailTemplate` och `ResetMailTemplate`
