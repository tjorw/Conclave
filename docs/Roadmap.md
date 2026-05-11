# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).


### Multitenancy

**Lokal dev och demo (R-MT018–R-MT019)**
- [ ] `R-MT018` SaaS-dev-workflow: `appsettings.SaaS.json`, SaaS-seeder med deterministiska tenant-IDs (Gammacon + Länsen), `Run-SaaSLocal.ps1`, `devTenantId` i Angular-environments, README-avsnitt
- [ ] `R-MT019` Demo SaaS-miljö: utöka `Run-DemoLocal.ps1` med `-EnableMultitenancy`-switch, SaaS-demo-databas, dokumentation i DemoDeploy.md

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [x] `R-MT013` `portal`-app: provisioneringsvy (systemadmin skapar konvent och admin-konto åt tenant)
- [ ] `R-MT014` `portal`-app: self-service signup – publik route `/signup`, `POST /system/public/signup`, subdomän-availability-query, välkomstmail via outbox
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare – ny `/tenant-login`-route, `isTenantOwner`-guard, dashboard med konventionsöversikt och kontoinställningar
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*
