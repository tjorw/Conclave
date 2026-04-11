# Roadmap – Conclave

Dokument för att spåra vad som är klart och vad som återstår inför produktionsstart och vidare.

---

## Nuläge (april 2026)

### Klar – backend-grund
- **Domänmodell** – alla fyra bounded contexts implementerade (Convention, Event, Registration, Staff) med aggregate roots, entiteter, value objects och domain events
- **CQRS-hanterare** – commands och queries för alla use cases (UC001–UC002b, UC003–UC012, UC-ST001–ST006, UC-TK001–TK004, UC-VR001–VR003, UC-SA001–SA007, UC-SR001–SR002, UC-EV001–EV011)
- **Infrastruktur** – EF Core med tre databaser (konventionsdatabas, systemdatabas, identitetsdatabas), EventDispatchInterceptor, DomainEventLog
- **Auth-stack** – JWT-middleware, ASP.NET Identity, `POST /auth/login`, tenant-resolution via `X-Convention-Id`-header
- **UC002** – identifiera eller skapa person vid inloggning
- **Minimal API** – endpoints för alla ovanstående use cases

### Ej klar
Se faserna nedan.

---

## Fas 1 – Systemet fungerar end-to-end

*Dessa delar krävs för att någon ska kunna logga in och använda systemet.*

### 1.1 Tenant-provisionering (blockerar allt annat)

UC001 skapar konventionen i konventionsdatabasen men registrerar den **inte** i systemdatabasen (`tenants`-tabellen). Utan den posten fungerar inte `X-Convention-Id`-headern och tenanten kan aldrig lösas.

**Vad som behövs:**
- `POST /auth/login` och UC001 förutsätter att en tenant-post finns i SystemDb med rätt connection string
- Antingen: UC001-handlern skapar tenant-posten automatiskt (kräver att handlern får tillgång till `SystemDbContext` eller en ny `ITenantRepository`)
- Eller: ett separat administrativt endpoint `POST /system/tenants` för att registrera en ny konvention i SystemDb
- Identity-konto måste också skapas för den registrerande personen (`ApplicationUser` i identitetsdatabasen)

**Beroenden:** Inget – kan tas direkt.

---

### 1.2 Profilkomplettering efter första inloggning

UC002 skapar en person med tomt namn. Registreringsflödena (UC-VR001, UC-SA001, UC-EV001) förutsätter att personen har ett namn och eventuellt telefonnummer.

**Vad som behövs:**
- Endpoint `PUT /me/profile` – autentiserad, uppdaterar den inloggade personens namn och telefon via `ICurrentUser.PersonId`
- Existerande `UpdatePersonCommand` kan återanvändas

**Beroenden:** Auth-stack (klar).

---

### 1.3 Rollbaserad auktorisering

Idag: alla autentiserade användare kan anropa alla muterande endpoints. En besökare kan t.ex. anropa `POST /editions/{id}/publish`.

Rollerna i systemet är inte traditionella JWT-roller – de härleds ur domäntillståndet:
- **Konventionsadministratör** – `ConventionAdministrator`-post finns för personens `PersonId`
- **Bemanningskoordinator** – person är bemanningskoordinator på en upplaga
- **Arrangemangskoordinator** – person är arrangemangskoordinator på en upplaga
- **Kategoriansvarig** – person är `ResponsibleId` på en kategori
- **Funktionsområdesansvarig** – person är `ResponsibleId` på ett funktionsområde

**Alternativ A (enkel, tillräcklig för v1):** Extrahera roller ur JWT vid utfärdande (lägg till claim `is_admin`, etc.) och kontrollera dem i endpoints.

**Alternativ B (korrekt, mer komplex):** Kontrollera domäntillståndet per request (ladda `Convention`, kolla `IsAdministrator(personId)`). Kan implementeras som en policy-baserad `IAuthorizationHandler`.

**Rekommendation för v1:** Alternativ A för admin-kontroll; resten kan vara öppet eller kontrolleras i handlers.

**Beroenden:** Inget – kan tas direkt, men kräver designbeslut.

---

### 1.4 Standardiserad felhantering

Idag returnerar servern 500 med undantagstext vid `InvalidOperationException`. Klienter behöver strukturerade felsvar.

**Vad som behövs:**
- `app.UseExceptionHandler` med ProblemDetails-format (RFC 7807)
- `InvalidOperationException` → 400/422
- `UnauthorizedAccessException` → 401
- `KeyNotFoundException` / null-aggregat → 404

**Beroenden:** Inget.

---

## Fas 2 – Operativa funktioner

*Dessa delar krävs för ett fullt fungerande system men blockerar inte tidig testning.*

### 2.1 E-postnotifikationer

Kritiska e-postflöden som saknas:
- Besöksregistrering bekräftad (UC-VR002)
- Staffansökan mottagen / accepterad / avslagen (UC-SA001, SA006, SA007)
- Evenemang godkänt / avvisat (UC-EV007, EV008)

**Vad som behövs:**
- `IEmailService`-interface i Application
- Implementation i Infrastructure (t.ex. SMTP eller SendGrid)
- Handlers för relevanta domain events

---

### 2.2 Publik feed-API

Externt CMS och publik vy behöver läsbara endpoints utan autentisering.

**Vad som behövs:**
- `GET /feed/editions/{id}` – schema, sessions, lokaler
- `GET /feed/events/{id}` – evenemangsdetaljer
- Anonyma endpoints, svarar med publik information

---

### 2.3 Integrationstester

Alla befintliga tester är enhets- och applikationstester mot mockade repositories. Ingen täckning av:
- Auth-flödet (inloggning, token-validering)
- Tenant-resolution
- EF Core-queries mot riktig databas

**Vad som behövs:**
- Testprojekt med Testcontainers eller lokal SQL Server
- Tester för UC002 (de viktigaste acceptanskriterierna kräver integration)

---

## Fas 3 – Frontend

*Angular-apparna är inte påbörjade.*

### 3.1 Admin-app (Angular)

- Rollbaserad (kräver att Fas 1.3 är klar)
- Hantering av konvention, upplaga, lokaler, funktionsområden, stationer, kategorier
- Bemanningsvy: pass, tilldelningar, staffansökningar
- Evenemangsgranskningsvy

### 3.2 Publik vy (Angular)

- Konventionsstyld (en app per konvention)
- Evenemangslista och detaljvy
- Registreringsflöden (besökare, staff, arrangör)
- Kräver Fas 2.2 (feed-API) och Fas 1.3

---

## Teknisk skuld

| Post | Beskrivning | Prioritet |
|------|-------------|-----------|
| Tenant-routing via domän | Idag: bara `X-Convention-Id`-header. Ska vara: lösa tenant via HTTP-domän (subdomän eller hostnamn) | Låg – header räcker för MVP |
| Social inloggning (OAuth) | ASP.NET Identity stöder det men inte implementerat | Låg |
| `CreatePersonCommand` vs UC002 | Två vägar att skapa en person (admin-väg och auth-väg). Kan leda till inkonsekvens om e-post-uniqueness-kontrollen blockerar auth-skapande | Medel – se till att UC002-vägen aldrig kolliderar |
| Tenant-databas-provisionering | Varje ny konvention behöver en ny SQL Server-databas. Processen för att skapa och migrera den är manuell | Medel |
| `appsettings` hemligheter | `Jwt:Key` ligger i `appsettings.Development.json`. Produktionsmiljö behöver Azure Key Vault, miljövariabler eller liknande | Hög inför produktion |
| Idempotens i login-flödet | Race condition: två parallella första-inloggningar kan försöka skapa person+länk simultaneously | Låg – unikt index är sista skyddet |

---

## Nästa konkreta steg (förslag)

1. **Fas 1.1** – Implementera tenant-provisionering: UC001 skapar också tenant-post i SystemDb + `ApplicationUser` i IdentityDb
2. **Fas 1.4** – Global felhantering med ProblemDetails (liten insats, stor vinst)
3. **Fas 1.2** – `PUT /me/profile` för profilkomplettering
4. **Fas 1.3** – Rollbaserad auktorisering (Alternativ A)
5. **Fas 2.1** – E-postnotifikationer
