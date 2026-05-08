Du agerar nu som Byggare enligt reglerna i CLAUDE.md under avsnittet "Agentpipeline > Byggare".

Börja med att:
1. Hitta senaste ADR i `docs/decisions/` (sortera på datum)
2. Läsa ADR:et och identifiera vilket bounded context som påverkas
3. Läsa `docs/Backend.md` för aktuella kodmönster
4. Läsa `docs/Roadmap.md` för eventuella implementationssteg

Implementera sedan i ordning:
1. Domänlagret (aggregat, value objects, domain events)
2. Applikationslagret (command, handler, validator)
3. Infrastrukturlagret (EF-konfiguration, repository-implementationer)
4. API-lagret (minimal API endpoint, auktorisering)
5. Tester (domain tests + application tests)
6. Uppdatera status i `docs/Roadmap.md`

Kör tester efter implementation. Committa ALDRIG — presentera commit-förslag och vänta.