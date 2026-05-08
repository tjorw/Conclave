Du orchestrerar nu pipeline: Bygg → Testa → Deploy-gate.

## Fas 1: Bygg
Kör:
  dotnet build backend/ConventionSystem.sln

Vid byggfel: stoppa, rapportera felet, vänta på instruktioner.

## Fas 2: Testa
Kör:
  dotnet test backend/ConventionSystem.sln --filter "FullyQualifiedName!~Integration"

Om tester failar:
- Analysera felmeddelanden
- Korrigera koden
- Kör om
- Max 3 försök. Vid 3 misslyckanden: pausa och presentera fullständig rapport för användaren.

Om frontend-kod ändrats, kör även:
  cd frontend && ng test --watch=false

## Fas 3: Deploy-gate
Om alla tester är gröna, presentera:
- Vilka filer som ändrats
- Vilka tester som körts och att de är gröna
- Commit-förslag enligt commit-strategin i CLAUDE.md

Skriv sedan:
"✅ Redo för deploy. Skriv 'godkänn deploy' för att fortsätta."

VÄNTA. Deployer inte förrän användaren explicit skriver "godkänn deploy".

## Fas 4: Deploy (efter godkännande)
Uppdatera acceptanskriterier i docs/UseCases.md ([  ] → [x]).
Kör commit med godkänt meddelande.
Rapportera klart.