# Roadmap – Conclave

Spårar vad som återstår inför produktionsstart.

**Regler:** `Rxx`-id är stabila och refereras i commits. Status: `[ ]` = ej startad, `[~]` = pågår, `[x]` = klar. Sortera efter prioritet (ej klara överst).


### Multitenancy

**Fas 4 – Provisioning och self-service (R-MT013–R-MT017)**
- [ ] `R-MT014` `portal`-app: self-service signup (publik del)
- [ ] `R-MT015` `portal`-app: tenant-dashboard för tenant-ägare
- [ ] `R-MT017` Faktureringsintegration *(utanför scope – dokumenterat för framtiden)*


---

## Teknisk skuld

Se ADR `docs/decisions/2026-05-10-tech-debt-triage.md` för fullständig triagering och motivering.

### Att åtgärda

- [x] `R-TD01` **E-postunikthet** — unikt DB-constraint på `persons(convention_id, email)` + felhantering i login och admin-skapande
- [x] `R-TD02` **Feed Cache-Control** — `Cache-Control: public, max-age=60/30` på feed-endpoints
- [x] `R-TD03` **DbSet-konsekvens** — lägg till `DbSet<Station>` och `DbSet<Venue>` i `ConventionDbContext`, ta bort `db.Set<T>()`-anrop

### Deferat (dokumenterat skäl)

| Post | Skäl |
|------|------|
| Cache stampede i `CachingTenantResolver` | Låg risk vid nuvarande skala; åtgärda vid mätbar DB-belastning |
| `Shift` saknar `EditionId` | Fungerar korrekt; ger schemaändringar utan omedelbar nytta |
| Deduplikering i tidsschema | Korrekt beteende; lägg till testtäckning om logiken ändras |
| Hemligheter i `appsettings` | Deployment-concern, dokumenterat i DemoDeploy.md; inte en kodfråga |
| Social inloggning (OAuth) | Feature request, inte skuld |
| E2E-tester | Infrastrukturkostnad motiveras efter att kritiska flöden stabiliserats |

