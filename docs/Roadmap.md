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

### Klar – Fas 1 (end-to-end)
- **1.1 Tenant-provisionering** – `POST /system/conventions` skapar konvention i ConventionDb, tenant-post i SystemDb, `ApplicationUser` i IdentityDb och `ConventionUserLink`
- **1.2 Profilkomplettering** – `PUT /me/profile` låter inloggad användare uppdatera namn, e-post och telefon
- **1.3 Rollbaserad auktorisering** – `is_admin`-claim i JWT, `IsAdmin`-policy, admin-endpoints skyddade; domänägarskapskontroller görs inline i handlers
- **1.4 Global felhantering** – `GlobalExceptionHandler` med ProblemDetails (RFC 7807): `ArgumentException` → 400, `InvalidOperationException` → 422, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404

### Klar – Fas 2
- **2.1 E-postnotifikationer** – `IEmailService` med handlers för `VisitorRegistrationConfirmed`, `StaffApplicationReceived/Accepted/Rejected`, `VersionApproved/Rejected`; `LoggingEmailService` som platshållare tills SMTP/SendGrid kopplas in
- **2.2 Publik feed-API** – `GET /feed/editions/{id}` och `GET /feed/events/{id}`, anonyma, filtrerar bort intern data
- **2.3 Integrationstester** – 14 tester mot SQL Server (Testcontainers), täcker tenant-resolution, UC002, auth-flödet och publik feed; per-test isolerade databaser via `ProvisionAsync`

### Ej klar
Se faserna nedan.

---

## ~~Fas 1 – Systemet fungerar end-to-end~~ ✓ Klar

*Alla delar implementerade – se "Nuläge" ovan.*

---

## ~~Fas 2 – Operativa funktioner~~ ✓ Klar

### ~~2.1 E-postnotifikationer~~ ✓ Klar
### ~~2.2 Publik feed-API~~ ✓ Klar
### ~~2.3 Integrationstester~~ ✓ Klar

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

1. **Fas 3.1** – Admin-app (Angular)
2. **Fas 3.2** – Publik vy (Angular)
