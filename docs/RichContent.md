# Rikt innehåll – design och arkitektur

Dokumentet täcker de tekniska besluten för R-RC01–R-RC04: markdown i eventbeskrivningar, bilduppladdning, redaktionella informationssidor och adminredigerbara mailmallar.

---

## Markdown

### Lagring
All markdown lagras som råtext i databasen. HTML genereras aldrig på servern för markdown-innehåll som redigeras av användare (eventbeskrivningar, sidor). Undantag: mailmallar renderas till HTML på servern vid utskick (se nedan).

### Rendering i frontend
Båda Angular-apparna (`admin` och `public`) använder biblioteket `ngx-markdown`, som i sin tur wraper `marked` + `DOMPurify`. `DOMPurify` saniterar outputen och förhindrar att rå HTML-taggar i indata renderas.

### Editor-mönster (admin)
Ingen WYSIWYG-editor. Formulärkomponenter som redigerar markdown-fält använder ett enkelt split-layout:
- Vänster kolumn: `<mat-form-field>` med `<textarea>` kopplad till reactive form-kontrollen
- Höger kolumn: `<markdown [data]="control.value" />` uppdateras i realtid

Mönstret upprepas inline i varje formulärkomponent; ingen delad abstraktionskomponent.

---

## Bilduppladdning (R-RC02)

### Abstraktion
Application-lagret definierar ett gränssnitt:

```csharp
// Application/Common/IFileStorage.cs
public interface IFileStorage
{
    Task<string> UploadAsync(
        string tenantId,
        string originalFilename,
        Stream content,
        string contentType,
        CancellationToken ct = default);
}
```

Metoden returnerar den publika URL:en till den uppladdade filen.

### Implementationer

| Klass | Provider | Status |
|---|---|---|
| `LocalDiskFileStorage` | Lokal disk (`wwwroot/uploads/{tenantId}/`) | Implementeras i R-RC02 |
| `BlobFileStorage` | Azure Blob Storage | Stub i R-RC02, implementeras separat |

Aktiv implementation väljs i `Program.cs` via konfigurationsnyckel `FileStorage:Provider = "Local" | "Blob"`.

### Filnamn och sökväg
Varje uppladdad fil får ett nytt GUID-baserat filnamn (`{guid}{.ext}`) för att undvika kollisioner. Sökvägen inkluderar `tenantId` för att garantera att filer från ett tenant aldrig kan skrivas över av ett annat, även om de delar samma URL-space:

```
Filsystem: wwwroot/uploads/{tenantId}/{guid}.jpg
URL:        /uploads/{tenantId}/{guid}.jpg
```

Statisk filmiddleware i `Program.cs` servar filer under `/uploads` utan autentisering.

### Begränsningar
- Tillåtna MIME-typer: `image/jpeg`, `image/png`, `image/gif`, `image/webp`
- Max filstorlek: konfigurerbar via `FileStorage:MaxSizeMb` (standard: 5 MB)
- Validering sker i API-endpointen innan `IFileStorage` anropas

### API-endpoint
```
POST /api/uploads
Content-Type: multipart/form-data
Fält: file

Svar 200:  { "url": "/uploads/{tenantId}/{guid}.ext" }
Svar 400:  vid ogiltig filtyp eller för stor fil
Svar 401:  ej autentiserad
```

---

## Informationssidor (R-RC03)

### Domänmodell
`Page` är en aggregatrot i det nya bounded context `Content`.

```
Domain/Content/
  Aggregates/Page.cs
  PageId.cs
```

Nyckelegenskaper:

| Egenskap | Typ | Beskrivning |
|---|---|---|
| `Id` | `PageId` | Primärnyckel |
| `Slug` | `string` (max 200) | URL-vänlig identifierare, unik per scope |
| `Title` | `string` (max 300) | Sidans rubrik |
| `Content` | `string` (max 20 000) | Markdown-brödtext |
| `EditionId` | `EditionId?` | `null` = konventionsscopead; satt = upplagescopead |
| `IsPublished` | `bool` | `false` = inte synlig i publika appen |
| `CreatedAt` | `DateTimeOffset` | |
| `UpdatedAt` | `DateTimeOffset` | Uppdateras av `UpdateContent()` |

### Scope och slug-unikhet
Slug valideras unikt inom scope:
- Konventionsscopead (`EditionId = null`): unik bland alla konventionsscopade sidor
- Upplagescopead (`EditionId = X`): unik bland sidor med samma `EditionId`

Samma slug kan alltså existera i båda scopen. Vid publikt uppslag prioriteras upplagescopead sida (för aktiv edition) framför konventionsscopead.

### Databas
Tabell `pages` i `dbo`-schema. Index:
- `IX_pages_tenant_slug` på `(TenantId, EditionId, Slug)` – unikhetskontroll och uppslag

### Publikt API
Endpointen `/api/pages/{slug}` söker publicerade sidor. Sökning sker i två steg:
1. Upplagescopead sida för aktuell edition
2. Konventionsscopead sida

Om inget hittas returneras 404.

---

## Mailmallar (R-RC04)

### Mål
Adminredigerbara mailmallar som ersätter hårdkodade strängar i `EmailTemplates.cs`. Varje malltyp har alltid en hårdkodad standardmall att återgå till.

### Entitet
`MailTemplate` lagras i tabell `mail_templates`:

| Kolumn | Typ | Beskrivning |
|---|---|---|
| `template_key` | `nvarchar(100)` PK | Identifierare, t.ex. `"VisitorRegistrationConfirmed"` |
| `subject` | `nvarchar(500)` | Ämnesrad med valfria `{{variabler}}` |
| `body_markdown` | `nvarchar(max)` | Brödtext i markdown med valfria `{{variabler}}` |
| `is_customized` | `bit` | `false` = standardmall aktiv, `true` = anpassad |

`MailTemplate` hanteras som CRUD-entitet i Application-lagret, inte som aggregatrot i domänlagret.

### Standardmallar
Hårdkodad i `DefaultMailTemplates.cs` (Infrastructure):

```csharp
public static class DefaultMailTemplates
{
    public static readonly IReadOnlyDictionary<string, (string Subject, string BodyMarkdown)> All =
        new Dictionary<string, (string, string)>
        {
            ["VisitorRegistrationConfirmed"] = (
                "Bekräftelse – din plats är bokad",
                "Hej {{firstName}},\n\nDin bokning till **{{eventTitle}}** är bekräftad.\n\n..."),
            // ... en post per malltyp
        };
}
```

### Variabelsubstitution och rendering
`TemplateRenderer` i Infrastructure-lagret:

1. Laddar mall från databas via `IMailTemplateRepository`; om `IsCustomized = false` används `DefaultMailTemplates`
2. Substituerar `{{variabelnamn}}` med angivna värden via `Regex.Replace`; okänd variabel → tom sträng
3. Renderar markdown → HTML med **Markdig** (`MarkdownPipeline` utan raw HTML)

```csharp
// Application/Common/ITemplateRenderer.cs
public interface ITemplateRenderer
{
    Task<(string Subject, string HtmlBody)> RenderAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default);
}
```

### Integration med IEmailService
De befintliga implementationerna av `IEmailService` (SendGrid, SMTP, Logging) ersätter sina interna anrop till `EmailTemplates`-klassen med anrop till `ITemplateRenderer`. Metodsignaturerna på `IEmailService` behålls oförändrade.

### Malltyper (initiala)
Alla befintliga mailtyper i `IEmailService` ska ha en defaultmall:

| Nyckel | Tillfälle |
|---|---|
| `VisitorRegistrationConfirmed` | Besökarsbokning bekräftad |
| `StaffApplicationReceived` | Funktionärsansökan mottagen |
| `StaffApplicationAccepted` | Funktionärsansökan godkänd |
| `StaffApplicationRejected` | Funktionärsansökan avslagen |
| `EventApproved` | Evenemang godkänt |
| `EventRejected` | Evenemang returnerat med kommentar |
| `PasswordReset` | Lösenordsåterställning |
| `EmailConfirmation` | E-postbekräftelse |

### Restore-flöde
`POST /api/mail-templates/{key}/restore` anropar en command handler som:
1. Slår upp standardmallen i `DefaultMailTemplates.All`
2. Skriver över `subject` och `body_markdown` i databasen
3. Sätter `is_customized = false`

---

## Beroenden och implementationsordning

```
R-RC01  Markdown i eventbeskrivningar
   │   ↓ lägger ngx-markdown i frontend
R-RC03  Informationssidor
   │   ↓ återanvänder editor-mönster och rendering
R-RC02  Bilduppladdning
   │   ↓ berikar RC01 och RC03 med bildstöd
R-RC04  Mailmallar  (oberoende av ovanstående; kopplas till R-OB01 när det är klart)
```
