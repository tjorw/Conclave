# UC001 – Skapa konvention

## Sammanfattning
En administratör skapar en ny konvention i systemet.

## Aktör
Systemadministratör

## Förutsättningar
- Inga

## Flöde
1. Administratören anger konventionens namn, slug samt sitt eget namn och e-post
2. Systemet validerar att slug är unikt bland alla konventioner
3. Systemet skapar konventionen
4. Systemet skapar ett personkonto för den registrerande användaren inom den nya konventionen
5. Systemet lägger till den personen som konventionsadministratör (med sig själv som addedById)
6. Systemet returnerar det nya ConventionId

## Affärsregler
- Slug måste vara unikt bland alla konventioner
- Slug får bara innehålla gemener, siffror och bindestreck
- Namn får inte vara tomt
- Ett personkonto är alltid kopplat till en konvention – den registrerande användarens person skapas som en del av detta flöde
- Den registrerande personen läggs automatiskt till som administratör

## Domänhändelser
- Convention Created, Person Created, Admin Added

## Acceptanskriterier
- [x] Konventionen sparas med ett giltigt ConventionId (Guid.CreateVersion7)
- [x] Ett personkonto skapas och kopplas till den nya konventionen
- [x] Den personen läggs till som administratör för konventionen
- [x] Dubblett-slug returnerar ett valideringsfel
- [x] Ogiltigt slug-format returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC002 – Identifiera eller skapa person vid registreringsflöde

## Sammanfattning
När en person deltar i något registreringsflöde (besökare, staff eller arrangör) identifierar systemet ett befintligt personkonto eller skapar ett nytt. Detta är inte en fristående operation – den sker alltid som en del av ett annat flöde (UC-VR001, UC-SA001, UC-EV001).

## Aktör
Valfri användare som startar ett registreringsflöde

## Förutsättningar
- Konventionen finns
- Användaren är autentiserad via identitetsleverantör (e-post/lösenord eller social inloggning)

## Flöde
1. Användaren autentiserar sig via identitetsleverantör
2. Systemet kontrollerar om ett personkonto finns för denna identitet inom konventionen
3a. Om personen finns: systemet kopplar sessionen till det befintliga personkontot
3b. Om personen inte finns: systemet skapar ett nytt personkonto kopplat till konventionen och den autentiserade identiteten
4. Registreringsflödet fortsätter

## Affärsregler
- Ett personkonto är alltid kopplat till konventionen (deploy-per-konvention – ingen delad identity)
- `ApplicationUser.PersonId` kopplar identitetskontot till `Person`-aggregatet; sätts vid första inloggningen
- Personskapandet är en bieffekt av autentisering – aldrig en fristående operation för slutanvändare
- Namn och telefon samlas in som en del av registreringsflödet om de inte redan finns på personkontot

## Domänhändelser
- Inga (personskapandet är infrastrukturellt, inte en domänhändelse)

## Acceptanskriterier
- [x] Befintlig person identifieras korrekt vid återinloggning
- [x] Nytt personkonto skapas vid första inloggning till en konvention
- [x] Person kopplas till korrekt ConventionId
- [x] Inga dubbla personkonton skapas för samma identitet inom en konvention

---

# UC002b – Hantera personregister

## Sammanfattning
En administratör skapar, uppdaterar eller avaktiverar personkonton i konventionens personregister.

## Aktör
Konventionsadministratör

## Förutsättningar
- Konventionen finns
- Utföraren är administratör för konventionen

## Flöde – Skapa
1. Administratören anger namn, e-post och valfritt telefonnummer
2. Systemet validerar att e-post är unikt inom konventionen
3. Systemet skapar personkontot
4. Systemet returnerar PersonId

## Flöde – Uppdatera
1. Administratören anger PersonId och uppdaterade fält (namn, e-post, telefon)
2. Systemet validerar e-postunikthet om e-post ändras
3. Systemet uppdaterar personkontot

## Flöde – Avaktivera
1. Administratören anger PersonId
2. Systemet markerar personen som inaktiv
3. Inaktiva personer kan inte initiera nya registreringar men befintlig data bevaras

## Affärsregler
- E-post måste vara unikt per konvention
- Avaktivering är mjuk – persondata raderas aldrig
- Administratörsskapade personer har kanske inget kopplat identitetskonto initialt

## Domänhändelser
- Person Created och Person Updated

## Acceptanskriterier
- [x] Person sparas och kopplas till korrekt ConventionId
- [x] Dubblett-e-post returnerar ett valideringsfel
- [x] Avaktiverad person kan inte initiera nya registreringar
- [x] Kommandohanterarna har tillhörande enhetstester

---

# UC003 – Lägg till konventionsadministratör

## Sammanfattning
En befintlig administratör ger administratörsrättigheter till en person inom en konvention.

## Aktör
Konventionsadministratör

## Förutsättningar
- Konventionen finns
- Personen finns inom konventionen
- Utföraren är administratör för konventionen

## Flöde
1. Administratören söker upp person via e-post inom konventionen
2. Systemet returnerar matchande person
3. Administratören bekräftar och ger admin-rättigheter till personen
4. Systemet validerar att personen tillhör konventionen
5. Systemet lägger till personen som administratör
6. Systemet registrerar vem som utförde åtgärden och när

## Affärsregler
- Bara befintliga administratörer kan lägga till nya administratörer
- En person kan bara läggas till som administratör en gång (idempotent eller valideringsfel)

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] ConventionAdministrator-post sparas med addedById och addedAt
- [x] Att lägga till en person som inte tillhör konventionen returnerar ett valideringsfel
- [x] Att lägga till en redan befintlig administratör hanteras korrekt
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC004 – Skapa upplaga

## Sammanfattning
En administratör skapar en ny upplaga av en konvention.

## Aktör
Konventionsadministratör

## Förutsättningar
- Konventionen finns
- Utföraren är administratör för konventionen

## Flöde
1. Administratören anger namn, startdatum, slutdatum, bemanningskoordinator och arrangemangskoordinator
2. Systemet validerar datumintervall (slutet måste vara efter starten)
3. Systemet skapar upplagan med status Utkast
4. Systemet returnerar det nya EditionId

## Affärsregler
- Slutdatum måste vara efter startdatum
- Upplagan skapas med status Utkast
- Bemanningskoordinator och arrangemangskoordinator måste vara personer som tillhör konventionen
- En upplaga kan inte publiceras utan tilldelade koordinatorer

## Domänhändelser
- Inga (upplagan skapas men publiceras ännu inte)

## Acceptanskriterier
- [x] Upplagan sparas med status Utkast och giltigt EditionId
- [x] Ogiltigt datumintervall returnerar ett valideringsfel
- [x] Koordinator som inte tillhör konventionen returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC005 – Publicera upplaga

## Sammanfattning
En administratör publicerar en upplaga, vilket gör den synlig och möjliggör att registreringsflöden kan öppnas.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns med status Utkast
- Upplagan har en bemanningskoordinator tilldelad
- Upplagan har en arrangemangskoordinator tilldelad

## Flöde
1. Administratören utlöser publicering
2. Systemet validerar alla förutsättningar
3. Systemet ändrar upplagens status till Publicerad
4. Systemet skickar EditionPublished-händelse

## Affärsregler
- Bara en Utkast-upplaga kan publiceras
- Bemanningskoordinator måste vara tilldelad
- Arrangemangskoordinator måste vara tilldelad
- När en upplaga väl är publicerad kan den inte återgå till Utkast

## Domänhändelser
- `EditionPublished { editionId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Upplagens status övergår till Publicerad
- [x] EditionPublished-händelse skickas
- [x] Publicering utan koordinatorer returnerar ett valideringsfel
- [x] Publicering av en redan publicerad upplaga returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC006 – Kopiera struktur från föregående upplaga

## Sammanfattning
En administratör kopierar lokaler och stationer från en föregående upplaga till en ny upplaga som startpunkt.

## Aktör
Konventionsadministratör

## Förutsättningar
- Målupplagan finns med status Utkast
- Källupplagan finns och tillhör samma konvention
- Utföraren är administratör för konventionen

## Flöde
1. Administratören anger käll-EditionId och mål-EditionId
2. Systemet kopierar alla lokaler från källan till målet
3. Systemet kopierar alla funktionsområden och stationer från källan till målet
4. Systemet skickar StructureCopiedFromEdition-händelse

## Affärsregler
- Bara en Utkast-upplaga kan ta emot en kopierad struktur
- Källa och mål måste tillhöra samma konvention
- Kopiering skriver över befintliga lokaler och stationer på målupplagan
- Kategorier kopieras inte – de skapas separat per upplaga

## Domänhändelser
- `StructureCopiedFromEdition { targetId, sourceId, venueCount, staffAreaCount, stationCount, performedById, occurredAt }`

## Acceptanskriterier
- [x] Alla lokaler från källan sparas på målet med nya id:n
- [x] Alla stationer från källan sparas på målet med nya id:n
- [x] Kopiering till en publicerad upplaga returnerar ett valideringsfel
- [x] Källa och mål från olika konventioner returnerar ett valideringsfel
- [x] StructureCopiedFromEdition-händelse skickas
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC007 – Öppna registrering

## Sammanfattning
En administratör öppnar ett av de tre registreringsflödena (arrangör, staff, besökare) för en upplaga.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns med status Publicerad
- Det specifika registreringsflödet är inte redan öppet

## Flöde
1. Administratören anger vilket registreringsflöde som ska öppnas (Arrangör | Staff | Besökare)
2. Systemet validerar att upplagan är publicerad
3. Systemet markerar registreringstypen som öppen
4. Systemet skickar RegistrationOpened-händelse

## Affärsregler
- Registrering kan bara öppnas på en publicerad upplaga
- Varje registreringstyp (arrangör, staff, besökare) öppnas oberoende av varandra
- Det finns ingen ordningsregel mellan de tre typerna – vilken som helst kan öppnas först
- En registreringstyp kan inte öppnas två gånger

## Domänhändelser
- `RegistrationOpened { editionId, type: RegistrationType, performedById, occurredAt }`

## Acceptanskriterier
- [x] Rätt registreringsflagga sätts till true på upplagan
- [x] RegistrationOpened-händelse skickas med korrekt typ
- [x] Öppning av registrering på en Utkast-upplaga returnerar ett valideringsfel
- [x] Öppning av en redan öppen registreringstyp returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC008 – Skapa lokal

## Sammanfattning
En administratör skapar en lokal (fysiskt rum eller utrymme) för en upplaga.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Utföraren är administratör för konventionen

## Flöde
1. Administratören anger namn och byggnad
2. Systemet skapar lokalen kopplad till upplagan
3. Systemet returnerar det nya VenueId

## Affärsregler
- Namn får inte vara tomt
- Lokalen är kopplad till en upplaga

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Lokalen sparas och kopplas till korrekt EditionId
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC009 – Skapa station (ursprunglig, ersatt av UC009 reviderad)

## Sammanfattning
En administratör skapar en bemanningsstation (t.ex. reception, kök, städ) för en upplaga.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Ansvarig person finns och tillhör konventionen

## Flöde
1. Administratören anger namn, beskrivning och ansvarig PersonId
2. Systemet skapar stationen kopplad till upplagan
3. Systemet returnerar det nya StationId

## Affärsregler
- Namn får inte vara tomt
- Ansvarig person måste tillhöra konventionen

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Stationen sparas och kopplas till korrekt EditionId
- [x] Ansvarig person som inte tillhör konventionen returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC010 – Skapa kategori

## Sammanfattning
En konventionsadministratör skapar en arrangemangskategori (t.ex. brädspel, rollspel, auktion) och tilldelar en ansvarig person.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Ansvarig person finns och tillhör konventionen

## Flöde
1. Administratören anger namn, beskrivning och ansvarig PersonId
2. Systemet skapar kategorin kopplad till upplagan
3. Systemet returnerar det nya CategoryId

## Affärsregler
- Namn får inte vara tomt
- Ansvarig person måste tillhöra konventionen
- En person kan vara ansvarig för flera kategorier

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Kategorin sparas och kopplas till korrekt EditionId
- [x] Ansvarig person som inte tillhör konventionen returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC011 – Byt kategoriansvarig

## Sammanfattning
En konventionsadministratör tilldelar om den ansvariga personen för en kategori.

## Aktör
Konventionsadministratör

## Förutsättningar
- Kategorin finns
- Den nya ansvariga personen finns och tillhör konventionen

## Flöde
1. Administratören anger CategoryId och ny ansvarig PersonId
2. Systemet validerar att den nya ansvariga personen tillhör konventionen
3. Systemet uppdaterar ansvarig person på kategorin

## Affärsregler
- Ny ansvarig person måste tillhöra konventionen

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Kategoriansvarig uppdateras
- [x] Ny ansvarig som inte tillhör konventionen returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC012 – Skapa funktionsområde

## Sammanfattning
En konventionsadministratör skapar ett bemanningsfunktionsområde (t.ex. reception, kök, städ) under en upplaga och tilldelar en ansvarig person. Den ansvarige kan administrera alla stationer och pass inom området.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Ansvarig person finns och tillhör konventionen
- Utföraren är administratör för konventionen

## Flöde
1. Administratören anger namn, valfri beskrivning och ansvarig PersonId
2. Systemet validerar att den ansvariga personen tillhör konventionen
3. Systemet skapar funktionsområdet kopplat till upplagan
4. Systemet returnerar det nya StaffAreaId

## Affärsregler
- Namn får inte vara tomt
- Ansvarig person måste tillhöra konventionen
- En person kan vara ansvarig för flera funktionsområden

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Funktionsområdet sparas och kopplas till korrekt EditionId
- [x] Ansvarig person som inte tillhör konventionen returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC009 (reviderad) – Skapa station

## Sammanfattning
En administratör skapar en station (t.ex. "Reception A") under ett funktionsområde för en upplaga.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Upplagan finns
- Funktionsområdet finns och tillhör upplagan
- Utföraren är administratör, bemanningskoordinator eller ansvarig för funktionsområdet

## Flöde
1. Administratören anger namn, valfri beskrivning och StaffAreaId
2. Systemet skapar stationen kopplad till funktionsområdet
3. Systemet returnerar det nya StationId

## Affärsregler
- Namn får inte vara tomt
- Station är kopplad till ett funktionsområde (och därigenom en upplaga)
- Station har inte längre en egen ansvarig person – funktionsområdesansvarig styr alla stationer inom området

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Stationen sparas och kopplas till korrekt StaffAreaId
- [x] StaffAreaId som inte tillhör upplagan returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST001 – Skapa pass

## Sammanfattning
En bemanningskoordinator eller funktionsområdesansvarig skapar ett pass (tidslucka med bemanningskrav) för en station.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Stationen finns och tillhör upplagan
- Passansvarig person finns och tillhör konventionen
- Utföraren är administratör, bemanningskoordinator eller ansvarig för stationens funktionsområde

## Flöde
1. Aktören anger StationId, starttid, sluttid, min antal, max antal och passansvarig PersonId
2. Systemet validerar tidsintervall och bemanningskrav
3. Systemet skapar passet med status Planerat
4. Systemet returnerar det nya ShiftId

## Affärsregler
- Sluttid måste vara efter starttid
- MaxPersons måste vara >= MinPersons
- MinPersons måste vara >= 0
- Passansvarig kan vara vilken person som helst som tillhör konventionen
- Pass skapas med status Planerat

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Passet sparas med status Planerat och korrekt StationId
- [x] Ogiltigt tidsintervall returnerar ett valideringsfel
- [x] Ogiltigt bemanningskrav returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST002 – Tilldela person till pass

## Sammanfattning
En bemanningskoordinator eller funktionsområdesansvarig tilldelar en person till ett pass. Det primära scenariot är att tilldela personer som skickat in en staffansökan, men vilken person som helst i konventionen kan tilldelas.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Passet finns med status Planerat
- Personen finns och tillhör konventionen

## Flöde
1. Aktören anger ShiftId och PersonId
2. Systemet validerar att passet inte är inställt och har ledig kapacitet
3. Systemet kontrollerar om personen har överlappande pass (varning, blockerar inte)
4. Systemet skapar tilldelningen med status Tilldelad
5. Systemet returnerar det nya StaffAssignmentId

## Affärsregler
- Passet får inte vara inställt
- Maxkapaciteten får inte överskridas
- En person kan inte tilldelas samma pass två gånger
- Tidsöverlapp med andra pass är en varning, inte ett hårt stopp
- Vilken person som helst i konventionen kan tilldelas (inte begränsat till staffsökande)

## Domänhändelser
- `PersonAssignedToShift { assignmentId, shiftId, personId, assignedById, occurredAt }`

## Acceptanskriterier
- [x] Tilldelningen sparas med status Tilldelad
- [x] Tilldelning till ett inställt pass returnerar ett valideringsfel
- [x] Tilldelning utöver maxkapacitet returnerar ett valideringsfel
- [x] Tilldelning av samma person två gånger returnerar ett valideringsfel
- [x] PersonAssignedToShift-händelse skickas
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST003 – Bekräfta tilldelning

## Sammanfattning
En bemanningskoordinator eller funktionsområdesansvarig bekräftar en bemanningstilldelning.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Tilldelningen finns med status Tilldelad

## Flöde
1. Aktören anger StaffAssignmentId
2. Systemet övergår tilldelningens status till Bekräftad
3. Systemet skickar AssignmentConfirmed-händelse

## Affärsregler
- Bara en Tilldelad tilldelning kan bekräftas

## Domänhändelser
- `AssignmentConfirmed { assignmentId, shiftId, personId, occurredAt }`

## Acceptanskriterier
- [x] Tilldelningens status övergår till Bekräftad
- [x] AssignmentConfirmed-händelse skickas
- [x] Bekräftelse av en icke-Tilldelad tilldelning returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST004 – Neka tilldelning

## Sammanfattning
En bemanningskoordinator eller funktionsområdesansvarig nekar en bemanningstilldelning.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Tilldelningen finns med status Tilldelad

## Flöde
1. Aktören anger StaffAssignmentId
2. Systemet övergår tilldelningens status till Nekad
3. Systemet skickar AssignmentRejected-händelse

## Affärsregler
- Bara en Tilldelad tilldelning kan nekas

## Domänhändelser
- `AssignmentRejected { assignmentId, shiftId, personId, occurredAt }`

## Acceptanskriterier
- [x] Tilldelningens status övergår till Nekad
- [x] AssignmentRejected-händelse skickas
- [x] Nekande av en icke-Tilldelad tilldelning returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST005 – Avboka tilldelning

## Sammanfattning
En bemanningskoordinator, funktionsområdesansvarig eller den tilldelade personen själv avbokar en bemanningstilldelning.

## Aktör
Konventionsadministratör, bemanningskoordinator, funktionsområdesansvarig eller den tilldelade personen

## Förutsättningar
- Tilldelningen finns med status Tilldelad eller Bekräftad

## Flöde
1. Aktören anger StaffAssignmentId
2. Systemet validerar att aktören antingen är behörig bemanningsadmin eller den tilldelade personen
3. Systemet övergår tilldelningens status till Avbokad
4. Systemet skickar AssignmentCancelled-händelse

## Affärsregler
- En Tilldelad eller Bekräftad tilldelning kan avbokas
- En Nekad eller redan Avbokad tilldelning kan inte avbokas
- Den tilldelade personen kan avboka sin egen tilldelning
- Administratörer, bemanningskoordinatorer och funktionsområdesansvariga kan avboka valfri tilldelning inom sitt område

## Domänhändelser
- `AssignmentCancelled { assignmentId, shiftId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Tilldelningens status övergår till Avbokad
- [x] AssignmentCancelled-händelse skickas
- [x] Avbokning av en redan avbokad eller nekad tilldelning returnerar ett valideringsfel
- [x] Den tilldelade personen kan avboka sin egen tilldelning
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-ST006 – Ställ in pass

## Sammanfattning
En bemanningskoordinator eller funktionsområdesansvarig ställer in ett helt pass. Alla aktiva tilldelningar avbokas automatiskt via en domänhändelsehanterare.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Passet finns med status Planerat

## Flöde
1. Aktören anger ShiftId
2. Systemet övergår passens status till Inställt
3. Systemet skickar ShiftCancelled-händelse
4. Domänhändelsehanterare avbokar alla aktiva tilldelningar på passet

## Affärsregler
- Bara ett Planerat pass kan ställas in
- Alla Tilldelade och Bekräftade tilldelningar avbokas som bieffekt

## Domänhändelser
- `ShiftCancelled { shiftId, stationId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Passens status övergår till Inställt
- [x] ShiftCancelled-händelse skickas
- [x] Inställning av ett redan inställt pass returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-TK001 – Skapa biljetttyp

## Sammanfattning
En administratör skapar en `TicketType` för en `Edition`, med pris, förmåner, tidsbegränsning och kategoribegränsning.

## Aktör
Konventionsadministratör

## Förutsättningar
- `Edition` finns
- Utförande användare är administratör för konventet

## Flöde
1. Administratören anger namn, pris, typ (`Visitor | Organiser | Volunteer`), samt valfritt `validDays` och `allowedCategories`
2. Systemet skapar `TicketType` kopplad till `Edition`
3. Systemet returnerar nytt `TicketTypeId`

## Affärsregler
- Namn får inte vara tomt
- Pris måste vara noll eller högre
- `validDays: null` innebär att biljetten gäller hela upplagan
- `allowedCategories: null` innebär att biljetten ger tillgång till alla kategorier
- Angivna dagar i `validDays` måste ligga inom `Edition`s datumintervall

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] `TicketType` persisteras och kopplas till korrekt `EditionId`
- [ ] `validDays` utanför `Edition`s datumintervall ger valideringsfel
- [ ] `TicketType` med `null` på både `validDays` och `allowedCategories` behandlas som obegränsad
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-TK002 – Lägg till förmån på biljetttyp

## Sammanfattning
En administratör lägger till en förmånsbeskrivning på en `TicketType`, t.ex. "T-shirt" eller "Matkupong dag 1".

## Aktör
Konventionsadministratör

## Förutsättningar
- `TicketType` finns och tillhör konventet

## Flöde
1. Administratören anger `TicketTypeId` och beskrivning av förmån
2. Systemet lägger till förmånen på `TicketType`

## Affärsregler
- Beskrivning får inte vara tom
- En `TicketType` kan ha noll eller flera förmåner

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] `TicketPerk` persisteras och kopplas till korrekt `TicketTypeId`
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-TK003 – Tilldela biljett till person

## Sammanfattning
En administratör eller ansvarig tilldelar en `Ticket` till en person. Används för arrangörs- och funktionärsbiljetter samt administrativa korrigeringar.

## Aktör
Konventionsadministratör, `EventCoordinator` (arrangörsbiljetter), `VolunteerCoordinator` (funktionärsbiljetter)

## Förutsättningar
- `Edition` finns och är publicerad
- `TicketType` finns och tillhör `Edition`
- `Person` finns och tillhör konventet

## Flöde
1. Aktören anger `PersonId` och `TicketTypeId`
2. Systemet skapar en `Ticket` med status `Reserved`
3. Systemet registrerar vem som tilldelade biljetten (`assignedById`)
4. Systemet returnerar nytt `TicketId`

## Affärsregler
- En person kan ha flera biljetter för samma `Edition`
- Tilldelade biljetter startar med status `Reserved` – betalning registreras separat
- Utförande aktör måste ha rätt roll (administratör, `EventCoordinator` eller `VolunteerCoordinator`)

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] `Ticket` persisteras med status `Reserved` och korrekt `assignedById`
- [ ] Person som inte tillhör konventet ger valideringsfel
- [ ] `TicketType` som inte tillhör `Edition` ger valideringsfel
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-VR001 – Anmäl som besökare

## Sammanfattning
En besökare anmäler sig till en upplaga. Systemet skapar en väntande registrering och en reserverad biljett.

## Aktör
Besökare (autentiserad person)

## Förutsättningar
- Upplagan finns och har besöksregistrering öppen
- Personen finns och tillhör konventionen
- Biljetttypen finns, tillhör upplagan och har kategorin Besökare
- Personen har inte redan en aktiv registrering för denna upplaga

## Flöde
1. Besökaren anger EditionId, PersonId och TicketTypeId
2. Systemet validerar förutsättningarna
3. Systemet skapar en VisitorRegistration med status VäntarPåBetalning
4. Systemet skapar en Ticket med status Reserverad
5. Systemet returnerar det nya VisitorRegistrationId

## Affärsregler
- Upplagan måste ha besöksregistrering öppen
- En person kan inte ha mer än en aktiv (icke-avbokad) registrering per upplaga
- Biljetttypen måste tillhöra samma upplaga och ha typen `Visitor`

## Domänhändelser
- Inga (betalningsbekräftelsen utlöser den meningsfulla händelsen)

## Acceptanskriterier
- [x] VisitorRegistration sparas med status VäntarPåBetalning
- [x] Ticket sparas med status Reserverad och korrekt TicketTypeId
- [x] Registrering på en stängd upplaga returnerar ett valideringsfel
- [x] Dubblettregistrering (samma person + upplaga) returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-VR002 – Bekräfta besöksregistrering efter betalning

## Sammanfattning
Besöksregistreringen bekräftas när biljetten betalats. Betalningsbekräftelsen hanteras via UC-TK004 (manuell) eller UC-TK005 (webhook). VisitorRegistration-statusen uppdateras som en kaskadeffekt av `TicketPaid`.

## Aktör
System (via `TicketPaid`-händelsehanterare)

## Förutsättningar
- VisitorRegistration finns med status VäntarPåBetalning
- `TicketPaid`-händelse har publicerats för biljettkopplad till registreringen

## Flöde
1. `TicketPaid` tas emot av händelsehanterare
2. Systemet identifierar kopplad VisitorRegistration
3. Systemet anropar `VisitorRegistration.Confirm()`
4. Systemet sparar uppdateringen

## Affärsregler
- Bara en VäntarPåBetalning-registrering kan bekräftas
- Bekräftelsen drivs av `TicketPaid`, inte av ett direktkommando

## Domänhändelser
- `VisitorRegistrationConfirmed { registrationId, personId, editionId, occurredAt }`

## Acceptanskriterier
- [ ] VisitorRegistrations status övergår till Bekräftad när `TicketPaid` tas emot
- [ ] Dubbel händelse (idempotens) hanteras utan fel
- [ ] Händelsehanterare har tillhörande enhetstest

---

# UC-VR003 – Avboka besöksregistrering

## Sammanfattning
En besökare eller administratör avbokar en besöksregistrering och makulerar den tillhörande biljetten.

## Aktör
Besökare (egen registrering) eller konventionsadministratör

## Förutsättningar
- VisitorRegistration finns och är inte redan avbokad

## Flöde
1. Aktören anger VisitorRegistrationId och performedById
2. Systemet anropar VisitorRegistration.Cancel()
3. Systemet anropar Ticket.Revoke(performedById)
4. Båda sparas

## Affärsregler
- En redan avbokad registrering kan inte avbokas igen
- En makulerad biljett kan inte makuleras igen

## Domänhändelser
- `TicketRevoked { ticketId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] VisitorRegistrations status övergår till Avbokad
- [x] Tickets status övergår till Makulerad
- [x] Avbokning av en redan avbokad registrering returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-TK004 – Registrera manuell betalning

## Sammanfattning
En administratör eller receptionspersonal registrerar manuellt att en `Ticket` har betalats, för fall där betalning sker utanför det integrerade betalsystemet (t.ex. kontant eller faktura).

## Aktör
Konventionsadministratör, receptionspersonal

## Förutsättningar
- `Ticket` finns med status `Reserved`
- Utförande användare har rätt roll

## Flöde
1. Aktören anger `TicketId` och valfri betalningsreferens
2. Systemet ändrar biljettstatus från `Reserved` till `Paid`
3. Systemet registrerar betalningsreferens om angiven

## Affärsregler
- Endast en `Reserved` biljett kan markeras som betald via detta flöde
- En betald biljett kan inte avbokas av innehavaren

## Domänhändelser
- `TicketPaid { ticketId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Biljettstatus ändras till `Paid`
- [x] `TicketPaid` domänhändelse publiceras
- [x] Registrering av betalning på redan betald biljett ger valideringsfel
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-TK005 – Bekräfta betalning via betalningsintegration

## Sammanfattning
Systemet tar emot en betalningsbekräftelse från extern betalningsleverantör och markerar motsvarande `Ticket` som betald.

## Aktör
Externt betalsystem (webhook)

## Förutsättningar
- `Ticket` finns med status `Reserved`
- Betalningsreferens matchar en känd väntande betalning

## Flöde
1. Betalningsleverantören skickar webhook med betalningsreferens och status
2. Systemet identifierar motsvarande `Ticket`
3. Systemet ändrar biljettstatus till `Paid`
4. Systemet publicerar `TicketPaid`

## Affärsregler
- Endast en `Reserved` biljett kan övergå till `Paid` via detta flöde
- Duplicerade webhook-anrop för samma betalningsreferens hanteras idempotent
- Misslyckad eller avbruten betalning ändrar inte biljettstatus

## Domänhändelser
- `TicketPaid { ticketId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Biljettstatus ändras till `Paid` vid bekräftad betalning
- [x] `TicketPaid` domänhändelse publiceras
- [x] Duplicerad webhook hanteras idempotent (inget fel, ingen dubblerad statusändring)
- [x] Misslyckad betalning ändrar inte biljettstatus
- [x] Integrationshanterare har tillhörande enhetstest

---

# UC-TK006 – Avboka biljett (innehavare)

## Sammanfattning
En biljettinnehavare avbokar sin egen `Ticket` innan betalning har registrerats.

## Aktör
Biljettinnehavare (autentiserad person)

## Förutsättningar
- `Ticket` finns med status `Reserved`
- Utförande användare är biljettinnehavaren

## Flöde
1. Personen begär avbokning av sin biljett
2. Systemet validerar att biljetten har status `Reserved`
3. Systemet ändrar biljettstatus till `Revoked`

## Affärsregler
- En innehavare kan endast avboka sin egen biljett
- Endast en `Reserved` biljett kan avbokas av innehavaren
- En `Paid` biljett kan inte avbokas av innehavaren – endast av administratör

## Domänhändelser
- `TicketRevoked { ticketId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Biljettstatus ändras till `Revoked`
- [x] `TicketRevoked` domänhändelse publiceras
- [x] Avbokning av betald biljett som innehavare ger valideringsfel
- [x] Avbokning av annans biljett ger auktoriseringsfel
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-TK007 – Makulera biljett (administratör)

## Sammanfattning
En administratör makulerar en `Ticket` oavsett status, för att hantera exceptionella situationer. Alla `SessionRegistrations` kopplade till biljetten avbokas automatiskt som en kaskadeffekt.

## Aktör
Konventionsadministratör

## Förutsättningar
- `Ticket` finns
- Utförande användare är administratör för konventet

## Flöde
1. Administratören anger `TicketId` och valfri anledning
2. Systemet ändrar biljettstatus till `Revoked`
3. Systemet publicerar `TicketRevoked`
4. Händelsehanterare lyssnar på `TicketRevoked` och avbokar alla `SessionRegistrations` kopplade till biljetten

## Affärsregler
- Administratör kan makulera vilken biljett som helst oavsett status (`Reserved`, `Paid` eller `Collected`)
- Makulering är oåterkallelig
- Kaskadeffekten hanteras via händelsehanterare, inte direkt i kommandot

## Domänhändelser
- `TicketRevoked { ticketId, personId, performedById, occurredAt }`

## Kaskadeffekter (via händelsehanterare)
- Alla `SessionRegistrations` där `ticketId` matchar avbokas
- `SessionRegistrationCancelled` publiceras för varje berörd registrering

## Acceptanskriterier
- [x] Biljettstatus ändras till `Revoked` oavsett nuvarande status
- [x] `TicketRevoked` domänhändelse publiceras
- [x] Alla kopplade `SessionRegistrations` avbokas
- [x] `SessionRegistrationCancelled` publiceras för varje avbokad registrering
- [x] Kommandohanterare och händelsehanterare har tillhörande enhetstester

---

# UC-TK008 – Hämta ut biljett i receptionen

## Sammanfattning
Receptionspersonal registrerar att en person har hämtat ut sin `Ticket` fysiskt vid ankomst till konventet. Förmånerna visas så att rätt utrustning kan delas ut.

## Aktör
Receptionspersonal (konventionsadministratör eller tilldelad roll)

## Förutsättningar
- `Ticket` finns med status `Paid`
- Person är fysiskt närvarande på konventet

## Flöde
1. Receptionspersonal identifierar biljetten (via `PersonId`, e-post eller biljettreferens)
2. Personal bekräftar uthämtning
3. Systemet ändrar biljettstatus från `Paid` till `Collected`
4. Systemet registrerar vem som utförde uthämtningen och när
5. Systemet visar biljetttypens förmåner så att rätt saker kan delas ut (t-shirt, matkuponger etc.)

## Affärsregler
- Endast en `Paid` biljett kan hämtas ut
- Uthämtning är en engångshändelse – en `Collected` biljett kan inte hämtas ut igen
- Förmånslistan visas vid uthämtning för att guida receptionspersonalen

## Domänhändelser
- `TicketCollected { ticketId, personId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Biljettstatus ändras till `Collected`
- [x] `collectedById` och `collectedAt` registreras
- [x] `TicketCollected` domänhändelse publiceras
- [x] Uthämtning av obetald biljett ger valideringsfel
- [x] Uthämtning av redan uthämtad biljett ger valideringsfel
- [x] Förmåner returneras i kommandosvaret för visning i receptionen
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-TK009 – Validera biljett inför sessionsregistrering

## Sammanfattning
Innan en `SessionRegistration` skapas validerar systemet att personens `Ticket` ger tillgång till sessionen. Detta är en domäntjänst som anropas som en del av UC-SR001, inte ett fristående use case.

## Aktör
Systemet (anropas som del av UC-SR001 – Registrera sig på session)

## Förutsättningar
- Person har minst en `Ticket` för `Edition`
- `Session` finns och är aktiv

## Valideringsregler
1. Person måste ha minst en biljett med status `Paid` eller `Collected`
2. Biljetten måste gälla på sessionens datum:
   - `validDays == null` → alltid giltig
   - `validDays != null` → sessionens datum måste finnas i `validDays`
3. Biljetten måste ge tillgång till sessionens kategori:
   - `allowedCategories == null` → tillgång till alla kategorier
   - `allowedCategories != null` → sessionens `CategoryId` måste finnas i `allowedCategories`

## Affärsregler
- Om en person har flera biljetter räcker det att en är giltig
- Valideringen utförs av `RegistrationRuleService.ValidateTicket(ticketId, sessionId)`

## Domänhändelser
- Inga (endast validering)

## Acceptanskriterier
- [x] Giltig biljett returnerar lyckat resultat
- [x] Ingen betald biljett ger valideringsfel
- [x] Biljett vars `validDays` exkluderar sessionens datum ger valideringsfel
- [x] Biljett vars `allowedCategories` exkluderar sessionens kategori ger valideringsfel
- [x] Person med flera biljetter godkänns om minst en är giltig
- [x] Domäntjänst har tillhörande enhetstester

---

# UC-SA001 – Skicka in staffansökan

## Sammanfattning
En person skickar in en ansökan om att arbeta som staff vid en upplaga.

## Aktör
Valfri person som tillhör konventionen

## Förutsättningar
- Upplagan finns och har staffregistrering öppen
- Personen finns och tillhör konventionen
- Personen har inte redan en aktiv staffansökan för denna upplaga

## Flöde
1. Personen anger EditionId och en intressebeskrivning
2. Systemet hämtar PersonId från inloggad användare (`ICurrentUser`)
3. Systemet validerar förutsättningarna
4. Systemet skapar en StaffApplication med status Mottagen
5. Systemet returnerar det nya StaffApplicationId

## Affärsregler
- Upplagan måste ha staffregistrering öppen
- En person kan inte ha mer än en aktiv ansökan per upplaga
- Intressebeskrivning får inte vara tom
- Personidentitet i self-service-flödet hämtas server-side från inloggning, inte från klientpayload

## Domänhändelser
- `StaffApplicationReceived { applicationId, personId, editionId, occurredAt }`

## Acceptanskriterier
- [x] StaffApplication sparas med status Mottagen
- [x] StaffApplicationReceived-händelse skickas
- [x] Ansökan på en stängd upplaga returnerar ett valideringsfel
- [x] Dubblettansökan (samma person + upplaga) returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA002 – Lägg till tillgänglighet i staffansökan

## Sammanfattning
En sökande lägger till en tidslucka som anger när de kan arbeta.

## Aktör
Sökande (egen ansökan)

## Förutsättningar
- StaffApplication finns
- Tidsintervallet är giltigt (slut efter start)

## Flöde
1. Sökande anger StaffApplicationId, starttid och sluttid
2. Systemet anropar StaffApplication.AddAvailability(from, to)
3. Systemet returnerar det nya AvailabilityId

## Affärsregler
- Sluttid måste vara efter starttid

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Tillgängligheten sparas och kopplas till staffansökan
- [x] Ogiltigt tidsintervall returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA003 – Ta bort tillgänglighet från staffansökan

## Sammanfattning
En sökande tar bort en tidigare tillagd tillgänglighets-tidslucka.

## Aktör
Sökande (egen ansökan)

## Förutsättningar
- StaffApplication finns
- Tillgängligheten finns på ansökan

## Flöde
1. Sökande anger StaffApplicationId och AvailabilityId
2. Systemet anropar StaffApplication.RemoveAvailability(availabilityId)

## Affärsregler
- Tillgängligheten måste tillhöra ansökan

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Tillgängligheten tas bort från staffansökan
- [x] Borttagning av en icke-existerande tillgänglighet returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA004 – Lägg till stationsönskemål i staffansökan

## Sammanfattning
En sökande uttrycker önskemål om att arbeta vid en specifik station.

## Aktör
Sökande (egen ansökan)

## Förutsättningar
- StaffApplication finns
- Stationen finns och tillhör samma upplaga

## Flöde
1. Sökande anger StaffApplicationId och StationId
2. Systemet anropar StaffApplication.AddStationPreference(stationId)

## Affärsregler
- En station kan bara förekomma en gång per ansökan
- Stationen måste tillhöra samma upplaga som ansökan

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Stationsönskemål sparas på staffansökan
- [x] Tillägg av ett dubblettönskemål returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA005 – Ta bort stationsönskemål från staffansökan

## Sammanfattning
En sökande tar bort ett tidigare uttryckt stationsönskemål.

## Aktör
Sökande (egen ansökan)

## Förutsättningar
- StaffApplication finns
- Stationsönskemålet finns på ansökan

## Flöde
1. Sökande anger StaffApplicationId och StationId
2. Systemet anropar StaffApplication.RemoveStationPreference(stationId)

## Affärsregler
- Önskemålet måste finnas på ansökan

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Stationsönskemålet tas bort från staffansökan
- [x] Borttagning av ett icke-existerande önskemål returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA006 – Acceptera staffansökan

## Sammanfattning
En bemanningskoordinator accepterar en staffansökan och övergår den till status Bekräftad.

## Aktör
Konventionsadministratör eller bemanningskoordinator

## Förutsättningar
- StaffApplication finns med status Mottagen eller UnderGranskning

## Flöde
1. Bemanningskoordinatorn anger StaffApplicationId och performedById
2. Systemet övergår StaffApplications status till Bekräftad

## Affärsregler
- Bara Mottagna eller UnderGranskning-ansökningar kan accepteras

## Domänhändelser
- `StaffApplicationAccepted { applicationId, personId, editionId, occurredAt }`

## Acceptanskriterier
- [x] StaffApplications status övergår till Bekräftad
- [x] StaffApplicationAccepted-händelse skickas
- [x] Accepterande av en redan bekräftad eller avslagen ansökan returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SA007 – Avslå staffansökan

## Sammanfattning
En bemanningskoordinator avslår en staffansökan.

## Aktör
Konventionsadministratör eller bemanningskoordinator

## Förutsättningar
- StaffApplication finns med status Mottagen eller UnderGranskning

## Flöde
1. Bemanningskoordinatorn anger StaffApplicationId och performedById
2. Systemet övergår StaffApplications status till Avslagen

## Affärsregler
- Bara Mottagna eller UnderGranskning-ansökningar kan avslås

## Domänhändelser
- `StaffApplicationRejected { applicationId, personId, editionId, occurredAt }`

## Acceptanskriterier
- [x] StaffApplications status övergår till Avslagen
- [x] StaffApplicationRejected-händelse skickas
- [x] Avslagande av en redan avslagen eller bekräftad ansökan returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SR001 – Registrera för session

## Sammanfattning
En besökare med giltig biljett registrerar sig för en specifik session vid upplagan.

## Aktör
Besökare (autentiserad person med biljett)

## Förutsättningar
- Sessionen finns och har ledig kapacitet
- Personen har en giltig Betald eller Uthämtad biljett för samma upplaga
- Personen är inte redan registrerad för sessionen

## Flöde
1. Personen anger SessionId, PersonId och TicketId
2. Systemet validerar platstillgänglighet via RegistrationRuleService
3. Systemet validerar att biljetten ger tillgång till sessionen via `RegistrationRuleService.ValidateTicket(personId, sessionId)` (se UC-TK009)
4. Systemet skapar SessionRegistration med status Bekräftad
5. Systemet returnerar det nya SessionRegistrationId

## Affärsregler
- Sessionen måste ha ledig kapacitet (kontrolleras via kors-kontextfråga)
- Biljetten måste vara Betald eller Uthämtad och tillhöra samma upplaga som sessionen
- En person kan inte registrera sig för samma session två gånger

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] SessionRegistration sparas med status Bekräftad
- [x] Registrering när sessionen är full returnerar ett valideringsfel
- [x] Registrering med ogiltig biljett returnerar ett valideringsfel
- [x] Dubblettregistrering returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-SR002 – Avboka sessionsregistrering

## Sammanfattning
En besökare avbokar sin registrering för en session.

## Aktör
Besökare (egen registrering) eller konventionsadministratör

## Förutsättningar
- SessionRegistration finns och är inte redan avbokad

## Flöde
1. Aktören anger SessionRegistrationId
2. Systemet anropar SessionRegistration.Cancel()
3. Systemet sparar uppdateringen

## Affärsregler
- En redan avbokad registrering kan inte avbokas igen

## Domänhändelser
- `SessionRegistrationCancelled { registrationId, sessionId, personId, occurredAt }`

## Acceptanskriterier
- [x] SessionRegistrations status övergår till Avbokad
- [x] SessionRegistrationCancelled-händelse skickas
- [x] Avbokning av en redan avbokad registrering returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# Event-kontexten

---

# UC-EV001 – Skicka in evenemang

## Sammanfattning
En arrangör skapar ett nytt evenemang för en upplaga. Systemet skapar aggregatet med ett tomt utkast och sätter arrangören som huvudarrangör.

## Aktör
Arrangör (person registrerad i konventionen)

## Förutsättningar
- Upplagan finns och är publicerad
- Personen finns och tillhör konventionen
- Kategorin finns på upplagan

## Flöde
1. Arrangören anger EditionId, CategoryId och sitt PersonId
2. Systemet skapar ett Event-aggregat med status Utkast
3. Systemet returnerar det nya EventId

## Affärsregler
- Upplagan måste vara publicerad
- Kategorin måste tillhöra upplagan

## Domänhändelser
- `EventCreated { eventId, editionId, categoryId, leadOrganiserId, occurredAt }`

## Acceptanskriterier
- [x] Event sparas med status Utkast och korrekt kategori och arrangör
- [x] Event sparas med tomma innehållsfält (titel, beskrivning) redo att redigeras
- [x] Skapande på en opublicerad upplaga returnerar ett valideringsfel
- [x] Skapande med okänd kategori returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV002 – Redigera evenemangsutkast

## Sammanfattning
Arrangören uppdaterar titel, beskrivning och registreringstyp på evenemanget.

## Aktör
Huvudarrangör eller medarrangör

## Förutsättningar
- Evenemanget finns och har status Utkast
- Utföraren är huvud- eller medarrangör

## Flöde
1. Arrangören anger EventId, titel, beskrivning, registreringstyp (och eventuella drop-in-regler)
2. Systemet uppdaterar fälten direkt på Event-aggregatet
3. Systemet sparar ändringen

## Affärsregler
- Redigering är bara möjlig när evenemanget är i Utkast-läge
- Titel och beskrivning får inte vara tomma
- DropInRules krävs om registreringstyp är DropIn eller Combined

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Titel, beskrivning och registreringstyp uppdateras på evenemanget
- [x] Redigering av ett evenemang i granskning eller publicerat returnerar ett valideringsfel
- [x] Tom titel returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV003 – Lägg till sessionönskemål

## Sammanfattning
Arrangören lägger till ett önskemål om sessionstid och format i utkastet – en önskelista som kategoriansvarig kan (men inte måste) följa vid schemaläggningen.

## Aktör
Huvudarrangör eller medarrangör

## Förutsättningar
- Evenemanget finns och har status Utkast

## Flöde
1. Arrangören anger EventId, beskrivning, önskad duration (minuter), antal platser och starttyp
2. Systemet lägger till ett SessionRequest på evenemanget
3. Systemet returnerar det nya SessionRequestId

## Affärsregler
- SessionRequest kan bara läggas till när evenemanget har status Utkast
- Duration måste vara > 0

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] SessionRequest sparas på evenemanget med korrekt data
- [x] Tillägg när evenemang inte är i Utkast-status returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV004 – Ta bort sessionönskemål

## Sammanfattning
Arrangören tar bort ett sessionönskemål från utkastet.

## Aktör
Huvudarrangör eller medarrangör

## Förutsättningar
- Evenemanget finns och har status Utkast
- SessionRequest med angivet id finns på evenemanget

## Flöde
1. Arrangören anger EventId och SessionRequestId
2. Systemet tar bort önskemålet från evenemanget
3. Systemet sparar ändringen

## Affärsregler
- Borttagning är bara möjlig när evenemanget har status Utkast

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] SessionRequest tas bort från evenemanget
- [x] Borttagning av ett icke-existerande önskemål returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV005 – Lägg till medarrangör

## Sammanfattning
Huvudarrangören lägger till en annan person som medarrangör för evenemanget.

## Aktör
Huvudarrangör

## Förutsättningar
- Evenemanget finns
- Personen finns och tillhör konventionen
- Utföraren är huvudarrangör

## Flöde
1. Huvudarrangören anger EventId och PersonId för medarrangören
2. Systemet lägger till personen som CoOrganiser
3. Systemet sparar ändringen

## Affärsregler
- Samma person kan inte läggas till som medarrangör två gånger
- Personen måste tillhöra konventionen

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] CoOrganiser sparas på evenemanget
- [x] Dublettillägg returnerar ett valideringsfel
- [x] Tillägg av person från annan konvention returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV006 – Skicka in för granskning

## Sammanfattning
Arrangören skickar in evenemangets utkast för granskning av kategoriansvarig.

## Aktör
Huvudarrangör eller medarrangör

## Förutsättningar
- Evenemanget finns och har status Utkast
- Evenemanget har titel och beskrivning ifyllda

## Flöde
1. Arrangören anger EventId
2. Systemet anropar `SubmitForReview()` – evenemangets status övergår till UnderReview
3. Systemet sparar ändringen

## Affärsregler
- Bara ett Utkast-evenemang kan skickas in för granskning
- Titel och beskrivning måste vara ifyllda
- Utföraren måste vara huvudarrangör eller medarrangör för evenemanget

## Domänhändelser
- `EventSubmittedForReview { eventId, occurredAt }`

## Acceptanskriterier
- [x] Evenemangsstatus övergår till UnderReview
- [x] Inskickning utan titel returnerar ett valideringsfel
- [x] Inskickning av ett redan granskat evenemang returnerar ett valideringsfel
- [x] Obehörig utförare returnerar behörighetsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV007 – Godkänn evenemang

## Sammanfattning
Kategoriansvarig godkänner det inskickade evenemanget. Evenemanget publiceras och innehållet är låst för redigering.

## Aktör
Kategoriansvarig

## Förutsättningar
- Evenemanget finns och har status UnderReview
- Utföraren är kategoriansvarig för evenemangskategorin

## Flöde
1. Kategoriansvarig anger EventId
2. Systemet anropar `Approve(responsibleId)`
3. Evenemangets status övergår till Published
4. Systemet sparar ändringen

## Affärsregler
- Bara ett UnderReview-evenemang kan godkännas
- Utföraren måste vara kategoriansvarig för kategorin

## Domänhändelser
- `EventApproved { eventId, responsibleId, occurredAt }`

## Acceptanskriterier
- [x] Evenemangsstatus övergår till Published
- [x] Godkännande av ett icke-granskat evenemang returnerar ett valideringsfel
- [x] Obehörig utförare returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV008 – Avvisa evenemang

## Sammanfattning
Kategoriansvarig avvisar det inskickade evenemanget med en kommentar. Evenemangets status återgår till Utkast med innehållet intakt så arrangören kan revidera och skicka in igen.

## Aktör
Kategoriansvarig

## Förutsättningar
- Evenemanget finns och har status UnderReview
- Utföraren är kategoriansvarig för evenemangskategorin

## Flöde
1. Kategoriansvarig anger EventId och en kommentar
2. Systemet anropar `Reject(responsibleId, comment)`
3. Kommentaren sparas som EventComment
4. Evenemangets status återgår till Draft
5. Systemet sparar ändringen

## Affärsregler
- Bara ett UnderReview-evenemang kan avvisas
- Kommentar måste anges
- Utföraren måste vara kategoriansvarig

## Domänhändelser
- `EventRejected { eventId, responsibleId, occurredAt }`

## Acceptanskriterier
- [x] Evenemangsstatus återgår till Draft
- [x] Kommentaren sparas på evenemanget
- [x] Avvisning utan kommentar returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV009 – Schemalägg session

## Sammanfattning
Kategoriansvarig skapar en session för ett publicerat evenemang – tilldelar lokal, tidslucka och antal platser. Sessionönskemålen från arrangören är vägledande men inte bindande.

## Aktör
Kategoriansvarig

## Förutsättningar
- Evenemanget finns och har status Published
- Lokalen finns på upplagan
- Utföraren är kategoriansvarig för evenemangskategorin

## Flöde
1. Kategoriansvarig anger EventId, VenueId, start- och sluttid, maxplatser och starttyp
2. Systemet anropar `CreateSession(venueId, timeSlot, maxSeats, startType)`
3. Sessionen sparas med status Aktiv
4. Systemet returnerar det nya SessionId

## Affärsregler
- Schemaläggning kräver publicerat evenemang
- Sluttid måste vara efter starttid
- MaxSeats måste vara > 0
- Lokalen måste tillhöra upplagan

## Domänhändelser
- `SessionCreated { eventId, sessionId, venueId, occurredAt }`

## Acceptanskriterier
- [x] Session sparas med status Aktiv och korrekt tidslucka och lokal
- [x] Schemaläggning på ett icke-publicerat evenemang returnerar ett valideringsfel
- [x] Ogiltig tidslucka (slut ≤ start) returnerar ett valideringsfel
- [x] Lokal som inte tillhör upplagan returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV010 – Inaktivera session

## Sammanfattning
Kategoriansvarig inaktiverar en session, t.ex. om lokalen inte längre är tillgänglig. Aktiva sessionsregistreringar avbokas via domänhändelse.

## Aktör
Kategoriansvarig eller konventionsadministratör

## Förutsättningar
- Evenemanget finns
- Sessionen finns och är aktiv
- Utföraren är kategoriansvarig eller admin

## Flöde
1. Utföraren anger EventId och SessionId
2. Systemet anropar `DeactivateSession(sessionId, performedById)`
3. Sessionen får status Inaktiv
4. SessionDeactivated-händelse publiceras (används av Registration för att avboka registreringar)
5. Systemet sparar ändringen

## Affärsregler
- En redan inaktiv session kan inte inaktiveras igen
- Utföraren måste vara kategoriansvarig eller admin

## Domänhändelser
- `SessionDeactivated { sessionId, eventId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Sessionsstatus övergår till Inaktiv
- [x] SessionDeactivated-händelse skickas
- [x] Inaktivering av en redan inaktiv session returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV011 – Ställ in evenemang

## Sammanfattning
Kategoriansvarig eller administratör ställer in ett evenemang i sin helhet. EventCancelled-händelsen triggar avbokning av alla sessionsregistreringar.

## Aktör
Kategoriansvarig eller konventionsadministratör

## Förutsättningar
- Evenemanget finns och är inte redan inställt
- Utföraren är kategoriansvarig eller admin

## Flöde
1. Utföraren anger EventId
2. Systemet anropar `CancelEvent(responsibleId)`
3. Evenemangets status övergår till Cancelled
4. EventCancelled-händelse publiceras
5. Systemet sparar ändringen

## Affärsregler
- Ett redan inställt evenemang kan inte ställas in igen
- Utföraren måste vara kategoriansvarig eller admin

## Domänhändelser
- `EventCancelled { eventId, responsibleId, occurredAt }`

## Acceptanskriterier
- [x] Evenemangsstatus övergår till Cancelled
- [x] EventCancelled-händelse skickas
- [x] Inställning av ett redan inställt evenemang returnerar ett valideringsfel
- [x] Obehörig utförare returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV012 – Hantera ändringsförslag efter publicering

## Sammanfattning
När ett evenemang är publicerat kan arrangören lämna ett ändringsförslag som kommentar. Kategoriansvarig eller administratör svarar och markerar kommentaren som hanterad. Arrangören kvitterar därefter svaret.

## Aktör
Arrangör, kategoriansvarig eller konventionsadministratör

## Förutsättningar
- Evenemanget finns och har status Published
- Arrangören är huvudarrangör eller medarrangör för evenemanget
- Svarande är kategoriansvarig för evenemangets kategori eller admin

## Flöde – Arrangör lämnar kommentar
1. Arrangören anger EventId och kommentartext
2. Systemet validerar att utföraren är arrangör för evenemanget
3. Systemet lägger till en `EventComment` med status `New` och `RequiresHandling = true`
4. Systemet sparar ändringen

## Flöde – Admin/kategoriansvarig svarar
1. Utföraren anger EventId, CommentId och svarstext
2. Systemet validerar behörighet (kategoriansvarig eller admin)
3. Systemet uppdaterar kommentaren till status `Responded` och sparar svar, handläggare och tidpunkt
4. Systemet sparar ändringen

## Flöde – Arrangör kvitterar svar
1. Arrangören anger EventId och CommentId
2. Systemet validerar att utföraren är arrangör och kommentarförfattare
3. Systemet uppdaterar kommentaren till status `Acknowledged` och sätter kvitteringsmetadata
4. Systemet sparar ändringen

## Affärsregler
- Flödet gäller endast publicerade evenemang
- Kommentartext och svarstext får inte vara tomma
- Endast kommentarer med `RequiresHandling = true` kan svaras på eller kvitteras
- Endast kommentarens författare får kvittera kommentaren

## Domänhändelser
- Inga nya domänhändelser krävs i nuläget (statusförändringar lagras på kommentaren)

## Acceptanskriterier
- [x] Arrangör kan lämna ändringsförslag på publicerat evenemang
- [x] Obehörig användare får behörighetsfel vid kommentar/svar/kvittens
- [x] Admin eller kategoriansvarig kan svara på kommentar och markera den som hanterad
- [x] Arrangör kan kvittera svarad kommentar
- [x] Admin-listning visar antal öppna kommentarer per evenemang
- [x] Kommandohanterare och domänregler har tillhörande enhetstester

---

# Promotionkoder (Registration-kontexten)

---

# UC-PC001 – Skapa promotionkod

## Sammanfattning
En administratör skapar en promotionkod för en upplaga med rabattyp, värde och valfria begränsningar.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan existerar

## Flöde
1. Administratören anger kod, beskrivning, rabattyp och -värde, valfria begränsningar (maxInlösningar, giltighetstid, biljetttyper)
2. Systemet validerar att koden är unik för upplagan
3. `PromotionCode`-aggregat skapas. `PromotionCodeCreated` dispatches
4. Systemet returnerar det nya `promotionCodeId`

## Affärsregler
- Koden måste vara unik per `editionId` (unique index på `(EditionId, Code)`, Code lagras i uppercase)
- `discountValue` för `Percentage` måste vara i intervallet 0–100
- `ValidFrom` får inte vara senare än `ValidUntil`
- `DiscountType`: `Percentage | Fixed | Free`

## Domänhändelser
- `PromotionCodeCreated { promotionCodeId, editionId, code, createdById, occurredAt }`

## Acceptanskriterier
- [x] Administratör kan skapa en kampanjkod med alla fält
- [x] Duplikat kod för samma upplaga ger 422 med felkod `promotion_code_already_exists`
- [x] Fri biljett-kod skapar kod med `DiscountType = Free`
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-PC002 – Lista promotionkoder för en upplaga

## Sammanfattning
Administratören listar alla promotionkoder för en upplaga inklusive inlösningsstatistik.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan existerar

## Flöde
1. Systemet returnerar alla promotionkoder för upplagan, inklusive `redemptionCount` och `maxRedemptions`

## Affärsregler
- Inga

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Listan visar `redemptionCount` och `maxRedemptions` korrekt

---

# UC-PC003 – Lösa in promotionkod

## Sammanfattning
En autentiserad besökare löser in en promotionkod på en reserverad biljett. Fri biljett markeras direkt som betald utan betalningsflöde.

## Aktör
Autentiserad besökare

## Förutsättningar
- Besökaren har en `Ticket` i status `Reserved`

## Flöde
1. Besökaren anger kampanjkoden i betalningssteget
2. Systemet slår upp koden (versalokänslig matchning) för aktuell upplaga
3. `RegistrationRuleService.ValidatePromotionCode(...)` kontrollerar domänreglerna
4. `promotionCode.Redeem(personId, ticketTypeId, now)` anropas; `redemptionCount` ökas
5. `Ticket` uppdateras: `finalPrice` beräknas (golv 0), `promotionCodeRedemptionId` sätts
6. `PromotionCodeRedeemed` dispatches
7. Om `finalPrice == 0`: biljetten transiteras direkt till `Paid` utan betalningsflöde

## Affärsregler
- Koden måste vara aktiv (`isActive = true`)
- `redemptionCount` får inte överskrida `maxRedemptions` (om satt)
- Aktuell tid måste falla inom `validFrom`–`validUntil` (om satta)
- `ticketTypeId` måste finnas i `allowedTicketTypeIds` om listan är icke-tom
- `finalPrice` kan aldrig bli negativ – golv vid 0
- `Ticket` måste ha status `Reserved`
- Samma person kan lösa in samma kod flera gånger om `maxRedemptions` tillåter det

## Domänhändelser
- `PromotionCodeRedeemed { promotionCodeId, ticketId, personId, discountApplied, occurredAt }`

## Acceptanskriterier
- [x] Inlösning av fri biljett sätter `Ticket.Status = Paid` utan betalning
- [x] Inlösning av rabatt uppdaterar `finalPrice` korrekt
- [x] Inaktiv eller utgången kod ger 422 med korrekt felkod
- [x] Inlösning ökar `redemptionCount` med 1
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-PC004 – Deaktivera promotionkod

## Sammanfattning
En administratör deaktiverar en promotionkod. Befintliga inlösningar påverkas inte.

## Aktör
Konventionsadministratör

## Förutsättningar
- Koden existerar och är aktiv

## Flöde
1. Administratören deaktiverar koden
2. `promotionCode.Deactivate(performedById)` anropas; `isActive` sätts till `false`
3. `PromotionCodeDeactivated` dispatches
4. Befintliga inlösningar och `Ticket`s påverkas inte

## Affärsregler
- En deaktiverad kod kan inte återaktiveras via domänmodellen

## Domänhändelser
- `PromotionCodeDeactivated { promotionCodeId, performedById, occurredAt }`

## Acceptanskriterier
- [x] Deaktivering gör att koden inte kan lösas in
- [x] Befintliga inlösningar påverkas inte av deaktivering
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-PC005 – Visa inlösningshistorik för en promotionkod

## Sammanfattning
Administratören ser alla inlösningar för en promotionkod med person, biljett, tidpunkt och tillämpad rabatt.

## Aktör
Konventionsadministratör

## Förutsättningar
- Koden existerar

## Flöde
1. Systemet returnerar alla `PromotionCodeRedemption`-poster för koden med `personId`, `ticketId`, `redeemedAt` och `discountApplied`

## Affärsregler
- Inga

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Historiklistan visar korrekt `discountApplied` per inlösning

---

# Hjälpsystem (HL-kontexten – admin-klienten)

---

# UC-HL001 – Visa inline hjälptooltip

## Sammanfattning
Användaren hovrar eller fokuserar en ⓘ-ikon och en kort förklaringstext visas.

## Aktör
Admin (alla roller)

## Förutsättningar
- ⓘ-ikonen (`HelpTooltip`) finns bredvid ett UI-element

## Flöde
1. Användaren hovrar (desktop) eller trycker (touch) på ⓘ-ikonen
2. En tooltip visas med en kort förklaringstext (max ~120 tecken)
3. Tooltip stängs när användaren lämnar elementet eller trycker utanför

## Affärsregler
- Tooltip-texten hämtas från `help.labels.ts` via `HelpTooltipKey` – inga hårdkodade texter
- Tillgänglig: `role="tooltip"`, `aria-describedby` på värd-elementet
- Touch-interaktion fungerar (tryck öppnar, tryck utanför stänger)

## Acceptanskriterier
- [ ] Tooltip visas med rätt text för varje `HelpTooltipKey`
- [ ] Tillgänglighetsattribut sätts korrekt
- [ ] Touch-interaktion fungerar på mobil

---

# UC-HL002 – Visa expanderbar förklaringspanel

## Sammanfattning
En kollapsad `HelpPanel` under sidans page-header förklarar ett domänkoncept och kan länka vidare till hjälpdrawern.

## Aktör
Admin (alla roller)

## Förutsättningar
- Sidan har en `HelpPanel`-komponent

## Flöde
1. En kollapsad panel med rubriken "Vad är [term]?" visas under sidans page-header
2. Användaren klickar/trycker för att expandera
3. Panelen visar 2–4 meningar om konceptet samt en "Läs mer"-länk
4. "Läs mer" öppnar hjälpdrawern på rätt topic via `HelpService.open(topic)`
5. Expansionstillståndet sparas i `localStorage` per nyckel

## Affärsregler
- Erfarna användare ser inte panelen öppen varje gång – tillståndet persisteras via `localStorage`
- Texten hämtas från `help.labels.ts`

## Acceptanskriterier
- [ ] Expansionstillståndet sparas och återläses från `localStorage`
- [ ] "Läs mer" öppnar drawern på rätt topic

---

# UC-HL003 – Öppna hjälpdrawer via global knapp

## Sammanfattning
Användaren klickar hjälp-ikonen (?) i topbar/sidenav och drawern öppnas med kontextuellt innehåll för aktuell route.

## Aktör
Admin (alla roller)

## Förutsättningar
- Global hjälp-ikon finns i appens topbar eller sidenav

## Flöde
1. Användaren klickar hjälp-ikonen
2. `HelpService` slår upp aktuell route och väljer relevant `HelpTopic` via `HELP_ROUTE_MAP`
3. Drawern öppnas från höger med titeln för aktuellt topic och renderad Markdown
4. Om ingen mappning finns för aktuell route visas standardinnehåll ("Välkommen till Conclave")
5. Användaren stänger drawern med ✕-knappen, Escape-tangenten eller genom att klicka utanför

## Affärsregler
- Route-till-topic-mappningen är typkontrollerad: `HelpTopic` är en string literal union
- Drawern är tillgänglig: `role="dialog"`, `aria-label`, fokus-trap

## Acceptanskriterier
- [ ] Rätt topic väljs baserat på aktuell route
- [ ] Routes utan mappning faller tillbaka på standardinnehåll
- [ ] Drawern stängs med Escape-tangenten
- [ ] Fokus-trap aktiveras när drawern är öppen

---

# UC-HL004 – Navigera mellan topics i drawern

## Sammanfattning
Användaren navigerar mellan hjälptopics via interna länkar i Markdown-innehållet eller topic-väljaren.

## Aktör
Admin (alla roller)

## Förutsättningar
- Hjälpdrawern är öppen

## Flöde
1. Användaren klickar på en intern länk eller väljer ett topic i navigationen
2. Innehållet ersätts utan att drawern stängs; scroll-position återställs till toppen
3. En bakåt-knapp visas om användaren navigerat från ett annat topic

## Affärsregler
- Navigationshistorik hanteras internt av `HelpService`
- Djuplänkar fungerar: `HelpService.open('edition-lifecycle')` öppnar rätt topic direkt

## Acceptanskriterier
- [ ] Innehållet byts utan att drawern stängs
- [ ] Bakåt-knapp visas och fungerar efter navigering
- [ ] `HelpService.open(topic)` öppnar korrekt topic direkt
