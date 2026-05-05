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
- Passansvarig person har en godkänd staffansökan för upplagan
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
- Passansvarig måste ha en godkänd staffansökan för upplagan
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
En bemanningskoordinator eller funktionsområdesansvarig tilldelar en person till ett pass. Personen måste tillhöra konventionen och ha en godkänd staffansökan för upplagan.

## Aktör
Konventionsadministratör, bemanningskoordinator eller funktionsområdesansvarig

## Förutsättningar
- Passet finns med status Planerat
- Personen finns och tillhör konventionen
- Personen har en godkänd staffansökan för upplagan

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
- Personen måste ha en godkänd staffansökan för upplagan innan tilldelning kan göras

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

# UC-ST007 – Tilldela funktionärsbiljett

## Sammanfattning
En bemanningskoordinator tilldelar en funktionärsbiljett (`TicketTypeCategory.Staff`) till en godkänd funktionär. Funktionären ser biljetten i "Mina biljetter" utan möjlighet att själv avboka.

## Aktör
Konventionsadministratör, `StaffCoordinator`

## Förutsättningar
- Upplagan finns
- `TicketType` med `Category = Staff` finns och tillhör upplagan
- Personen är en godkänd funktionär för upplagan

## Flöde
1. Koordinatorn väljer funktionär och biljetttyp i admin-vyn för funktionärer
2. Systemet revokar eventuell befintlig aktiv funktionärsbiljett av annan typ
3. Systemet skapar ny `Ticket` med `AssignedById` satt
4. Funktionären ser biljetten i "Mina biljetter" i publika appen

## Affärsregler
- Byte av biljetttyp är atomärt (revoka + skapa)
- Samma biljetttyp som redan tilldelad → noop
- `TicketTypeId = null` → revoka utan ny biljett
- Funktionären kan inte själv avboka biljetten

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] `Ticket` skapas med `TicketTypeCategory.Staff`, `AssignedById` satt, status `Reserved`
- [x] Befintlig aktiv funktionärsbiljett av annan typ revokeras vid byte
- [x] Biljetttyp som inte tillhör `Edition` ger valideringsfel
- [x] Biljetttyp som inte är `Staff` ger valideringsfel
- [x] Utförare utan behörighet (varken admin eller StaffCoordinator) ger `ForbiddenException`
- [x] Funktionärsbiljetten syns i publika vyn ("Mina biljetter") utan avboka-åtgärd
- [x] Kommandohanterare har tillhörande enhetstest

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
- [x] `Ticket` persisteras med status `Reserved` och korrekt `assignedById`
- [x] Person som inte tillhör konventet ger valideringsfel
- [x] `TicketType` som inte tillhör `Edition` ger valideringsfel
- [x] Kommandohanterare har tillhörande enhetstest

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
1. Arrangören anger EditionId, CategoryId och sitt PersonId. Administratör kan ange en annan huvudarrangör.
2. Systemet skapar ett Event-aggregat med status Utkast
3. Systemet returnerar det nya EventId

## Affärsregler
- Upplagan måste vara publicerad
- Kategorin måste tillhöra upplagan
- Huvudarrangören måste vara den inloggade personen, utom när utföraren är administratör
- API:t tar inte emot `ConventionId` från request body; personens konvention valideras mot upplagans faktiska `ConventionId`

## Domänhändelser
- `EventCreated { eventId, editionId, categoryId, leadOrganiserId, occurredAt }`

## Acceptanskriterier
- [x] Event sparas med status Utkast och korrekt kategori och arrangör
- [x] Event sparas med tomma innehållsfält (titel, beskrivning) redo att redigeras
- [x] Skapande på en opublicerad upplaga returnerar ett valideringsfel
- [x] Skapande med okänd kategori returnerar ett valideringsfel
- [x] Icke-admin kan inte skapa evenemang åt annan `LeadOrganiserId`
- [x] `ConventionId` tas inte från request body
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV002 – Redigera evenemangsinnehåll

## Sammanfattning
Arrangören uppdaterar titel, beskrivning och registreringstyp på evenemanget.

## Aktör
Inloggad användare i arrangörsflödet

## Förutsättningar
- Evenemanget finns
- Evenemanget är inte inställt

## Flöde
1. Användaren anger EventId, titel, beskrivning, registreringstyp, eventuella drop-in-regler, schemaönskemålstext och önskat antal medarrangörer
2. Systemet uppdaterar fälten direkt på Event-aggregatet
3. Systemet sparar ändringen

## Affärsregler
- Inställda evenemang är skrivskyddade
- Titel och beskrivning får inte vara tomma

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Titel, beskrivning och registreringstyp uppdateras på evenemanget
- [x] Schemaönskemålstext och önskat antal medarrangörer uppdateras på evenemanget
- [x] Redigering av ett inställt evenemang returnerar ett valideringsfel
- [x] Tom titel returnerar ett valideringsfel
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV003 – Uppdatera schemaönskemål

## Sammanfattning
Arrangören beskriver eller rensar önskemål om sessionstid, format och andra schemaförutsättningar som fri text på evenemanget. Det är en önskelista som kategoriansvarig kan (men inte måste) följa vid schemaläggningen.

## Aktör
Inloggad användare i arrangörsflödet

## Förutsättningar
- Evenemanget finns
- Evenemanget är inte inställt

## Flöde
1. Användaren anger EventId och schemaönskemålstext
2. Systemet uppdaterar `ScheduleRequestText` på evenemanget. Tom eller whitespace-only text sparas som `null`
3. Systemet sparar ändringen

## Affärsregler
- Inställda evenemang är skrivskyddade

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Schemaönskemålstext sparas på evenemanget
- [x] Tom text rensar schemaönskemålet
- [x] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EV005 – Ansök om medarrangör via e-post

## Sammanfattning
Huvudarrangören föreslår en eller flera medarrangörer genom att ange e-postadress. Förslaget blir en väntande ansökan och ger inga arrangörsrättigheter förrän en behörig admin har godkänt den.

## Aktör
Huvudarrangör

## Förutsättningar
- Evenemanget finns
- Utföraren är huvudarrangör
- Arrangörsregistreringen är öppen eller evenemanget är fortfarande redigerbart för arrangören

## Flöde
1. Huvudarrangören anger EventId, e-postadress och valfritt namn/meddelande för medarrangören
2. Systemet normaliserar e-postadressen och kontrollerar att personen inte redan är huvudarrangör, aktiv medarrangör eller har en väntande ansökan för samma evenemang
3. Systemet skapar en `CoOrganiserApplication` med status `Pending`
4. Systemet sparar ansökan
5. Systemet gör ansökan synlig för admin/kategoriansvarig i granskningsvyn

## Affärsregler
- En väntande ansökan räknas inte som medarrangör
- Endast godkända medarrangörer får redigera evenemanget, skicka in det, synas publikt, få arrangörsschema eller ingå i arrangörsbiljettsflöden
- Samma e-postadress kan bara ha en aktiv eller väntande medarrangörskoppling per evenemang
- Huvudarrangörens e-postadress kan inte läggas till som medarrangör
- E-postadressen matchas mot person först vid godkännande, så arrangören behöver inte känna till ett `PersonId`

## Domänhändelser
- `CoOrganiserApplicationSubmitted { applicationId, eventId, email, requestedById, occurredAt }`

## Acceptanskriterier
- [x] Huvudarrangör kan skapa en väntande medarrangörsansökan med e-postadress
- [x] Väntande ansökan ger inte arrangörsbehörighet och visas inte som aktiv medarrangör
- [x] Dublett mot aktiv medarrangör eller väntande ansökan returnerar ett valideringsfel
- [x] Huvudarrangörens egen e-postadress kan inte nomineras
- [x] Huvudarrangör kan återkalla en väntande ansökan innan den granskats
- [x] Kommandohanteraren har tillhörande enhetstester

---

# UC-EV005b – Godkänn eller avslå medarrangör

## Sammanfattning
Admin eller kategoriansvarig granskar en väntande medarrangörsansökan. Först vid godkännande blir personen aktiv medarrangör och räknas i övriga arrangörsflöden.

## Aktör
Konventionsadministratör eller kategoriansvarig för evenemangets kategori

## Förutsättningar
- Evenemanget finns
- Medarrangörsansökan finns och har status `Pending`
- Utföraren har behörighet att granska evenemang i kategorin

## Flöde – godkänn
1. Admin väljer en väntande ansökan
2. Systemet matchar normaliserad e-post mot befintlig `Person` i samma konvention
3. Om personen saknas skapar systemet en personpost enligt UC002 utan att kräva att personen redan har loggat in
4. Systemet kontrollerar att personen inte är huvudarrangör eller redan aktiv medarrangör
5. Systemet markerar ansökan som `Approved`
6. Systemet lägger till personen som aktiv `CoOrganiser`
7. Systemet sparar ändringen

## Flöde – avslå
1. Admin väljer en väntande ansökan och anger valfri kommentar
2. Systemet markerar ansökan som `Rejected`
3. Systemet sparar ändringen utan att lägga till någon `CoOrganiser`

## Affärsregler
- Bara `Approved`-ansökningar får skapa aktiva `CoOrganiser`-poster
- Avslagna ansökningar kan inte godkännas senare; huvudarrangören får skapa en ny ansökan vid behov
- Godkännande är idempotent mot redan aktiv medarrangör: systemet ska inte skapa dubletter
- En godkänd medarrangör får samma arrangörsrättigheter som huvudarrangören där befintliga use cases säger "huvudarrangör eller medarrangör"
- Om en personpost skapas från e-post ska den tillhöra samma konvention och följa befintliga regler för e-post-unikhet

## Domänhändelser
- `CoOrganiserApplicationApproved { applicationId, eventId, personId, reviewedById, occurredAt }`
- `CoOrganiserApplicationRejected { applicationId, eventId, reviewedById, comment, occurredAt }`

## Acceptanskriterier
- [x] Admin/kategoriansvarig kan godkänna en väntande ansökan
- [x] Godkännande skapar eller återanvänder person och lägger till aktiv `CoOrganiser`
- [x] Avslag aktiverar inte medarrangören
- [x] Väntande och avslagna ansökningar räknas inte i `IsOrganiser`, `ListMyEvents`, arrangörsschema eller arrangörsbiljetter
- [x] Beslutsfattare och beslutstid sparas
- [x] Kommandohanterarna har tillhörande enhetstester

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

# UC-EV013 – Visa arrangörsbiljetter vid anmälan av arrangemang

## Sammanfattning
När arrangören öppnar formuläret för att anmäla ett arrangemang visar systemet vilka arrangörsbiljetter som finns för upplagan. Informationen är endast informativ och kan inte väljas i detta steg.

## Aktör
Arrangör (autentiserad)

## Förutsättningar
- Upplagan finns
- Arrangören är autentiserad
- Formuläret för att anmäla arrangemang kan öppnas

## Flöde
1. Arrangören öppnar formuläret för att anmäla ett arrangemang
2. Systemet hämtar alla `TicketType` för upplagan där `TicketTypeCategory = Organiser`
3. Systemet returnerar listan tillsammans med övrig evenemangsdata
4. Arrangören ser vilka arrangörsbiljetter som finns tillgängliga som informationstext

## Affärsregler
- Arrangörsbiljetter visas endast informativt i detta flöde
- Arrangören kan inte välja eller ansöka om en arrangörsbiljett här
- Om inga arrangörsbiljetter finns ska sektionen inte visas

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Arrangörsbiljetter visas i anmälningsformuläret i publika appen
- [x] Om inga arrangörsbiljetter finns visas inte sektionen
- [x] Arrangören kan inte välja eller ansöka om biljett i detta steg

---

# UC-EV014 – Tilldela arrangörsbiljett vid publicering av arrangemang

## Sammanfattning
När en administratör publicerar ett arrangemang kan arrangörsbiljetter tilldelas samtidigt till huvudarrangör och eventuella medarrangörer.

## Aktör
Kategoriansvarig eller evenemangskoordinator (admin)

## Förutsättningar
- Upplagan är Published
- Arrangemanget finns och har status UnderReview
- Utföraren har behörighet att publicera arrangemanget

## Flöde
1. Administratören öppnar publiceringsvyn för arrangemanget
2. Systemet visar tillgängliga `TicketType` där `TicketTypeCategory = Organiser`
3. Systemet visar nuvarande tilldelning per arrangör, inklusive co-organisers om sådana finns
4. För varje arrangör kan administratören välja en biljetttyp eller alternativet "Ingen biljett"
5. Administratören justerar tilldelningarna vid behov
6. Administratören bekräftar publicering
7. Systemet publicerar arrangemanget och skickar `EventPublished`
8. För varje arrangör som tilldelats biljett:
9. Om arrangören redan har en arrangörsbiljett för upplagan sätter systemet den gamla till `Revoked` och skapar en ny biljett
10. Om arrangören inte redan har en arrangörsbiljett skapar systemet en ny biljett med status `Reserved`
11. Systemet sparar allt i samma transaktion och returnerar bekräftelse

## Affärsregler
- En arrangör kan ha högst en arrangörsbiljett per upplaga åt gången
- Byte av biljett är atomärt: revoke och ny biljett sker i samma transaktion
- Det är valfritt att tilldela biljett; publicering kan ske utan tilldelning
- Co-organisers och huvudarrangör behandlas lika
- Om inga `TicketTypeCategory = Organiser`-typer finns visas inte biljettsektionen

## Domänhändelser
- `EventPublished { eventId, responsibleId, occurredAt }`
- `OrganizerTicketsAssigned { eventId, editionId, assignments, occurredAt }`

## Acceptanskriterier
- [x] Publiceringsvyn visar tillgängliga arrangörsbiljetter och nuvarande tilldelning
- [x] Systemet sparar korrekt med revoke + ny biljett vid byte
- [x] Publicering och biljetttilldelning sker i samma anrop och transaktion
- [x] Om inga `TicketTypeCategory = Organiser`-typer finns visas inte biljettsektionen

---

# UC-EV015 – Hantera arrangörsbiljett manuellt

## Sammanfattning
En konventionsadministratör kan manuellt tilldela, byta eller ta bort arrangörsbiljett för en arrangör utan att gå via publiceringsflödet.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Utföraren är konventionsadministratör
- Arrangören finns i konventionen

## Flöde – Tilldela eller byt
1. Administratören öppnar arrangörens registreringssida eller evenemangets admin-vy
2. Administratören väljer arrangör och en `TicketType` där `TicketTypeCategory = Organiser`
3. Systemet kontrollerar om arrangören redan har en arrangörsbiljett för upplagan
4. Om ja: systemet sätter befintlig biljett till `Revoked` och skapar en ny med status `Reserved`
5. Om nej: systemet skapar en ny biljett med status `Reserved`
6. Systemet sparar och bekräftar

## Flöde – Ta bort
1. Administratören väljer att ta bort arrangörsbiljetten
2. Systemet sätter biljettens status till `Revoked`
3. Systemet sparar och bekräftar

## Affärsregler
- Samma regel gäller som i UC-EV014: en arrangör kan bara ha en aktiv arrangörsbiljett per upplaga
- Manuell tilldelning kräver inte att arrangemanget är Published
- Revoked-biljetter visas inte för arrangören
- Historik bevaras genom att revokerade biljetter inte tas bort ur databasen

## Domänhändelser
- Inga nya krav utöver ordinarie biljett- och historikhändelser

## Acceptanskriterier
- [x] Admin kan tilldela, byta och ta bort arrangörsbiljett fristående från publiceringsflödet
- [x] Byte är atomärt
- [x] Logg och historik bevaras genom att revokerad biljett inte tas bort ur databasen

---

# UC-EV016 – Arrangör ser sin arrangörsbiljett

## Sammanfattning
En arrangör ser sin arrangörsbiljett i "Mina biljetter" tillsammans med övriga biljetter, men kan inte själv avboka den.

## Aktör
Arrangör (autentiserad)

## Förutsättningar
- Arrangören är autentiserad
- Arrangören har biljetter kopplade till sitt `PersonId` för aktuell upplaga

## Flöde
1. Arrangören öppnar "Mina biljetter" i publika appen
2. Systemet hämtar alla `Ticket` kopplade till arrangörens `PersonId` för aktuell upplaga
3. Biljetter vars `TicketType` har `TicketTypeCategory = Organiser` visas i listan tillsammans med övriga biljetter
4. Avboka-knappen visas inte för arrangörsbiljetter

## Affärsregler
- Arrangören kan inte själv avboka en arrangörsbiljett
- Revoked arrangörsbiljetter visas inte
- Rätt biljetttypsinformation ska visas, till exempel namn, giltiga dagar och förmåner

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Arrangörsbiljetten syns i biljettvyn tillsammans med övriga biljetter
- [x] Ingen avboka-åtgärd finns tillgänglig på arrangörsbiljetter
- [x] Rätt biljetttypsinformation visas

---

## Domänförändringar för arrangörsbiljetter

### TicketType (Registration BC)
- Ingår i `allowedCategories`-logiken som vanligt, men tilldelas aldrig via självregistrering

### Ticket (Registration BC)
- Arrangörsbiljetter är upplage-/personbaserade och har ingen koppling till ett enskilt arrangemang
- Ny metod: `static Ticket CreateOrganizerTicket(TicketTypeId, PersonId, EditionId)`
- Ny domänmetod på aggregatroten: `AssignOrganizerTicket(PersonId, TicketTypeId)` och `RevokeOrganizerTicket(PersonId)`

### Event (Event BC)
- `Publish()` tar en optional parameter `IReadOnlyList<OrganizerTicketAssignment>`
- Nytt value object: `record OrganizerTicketAssignment(PersonId PersonId, TicketTypeId? TicketTypeId)`
- Skickar `OrganizerTicketsAssigned` som domain event som Registration BC lyssnar på

### Kommunikation mellan BC
- `Event` ──`OrganizerTicketsAssigned`──▶ `Registration` för att skapa, byta eller revoka arrangörsbiljetter

## Beslutade regler
- Vid `EventCancelled` ska arrangörsbiljetten automatiskt revokeras om arrangören inte längre har några andra publicerade arrangemang i samma upplaga. Om arrangören fortfarande har andra publicerade arrangemang ska processen kräva ett explicit val för hur arrangörsbiljetten ska hanteras.
- Medarrangörer ska ha samma regler för arrangörsbiljetter som huvudarrangören.


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
- [x] Tooltip visas med rätt text för varje `HelpTooltipKey`
- [x] Tillgänglighetsattribut sätts korrekt
- [x] Touch-interaktion fungerar på mobil

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
- [x] Expansionstillståndet sparas och återläses från `localStorage`
- [x] "Läs mer" öppnar drawern på rätt topic

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
- [x] Rätt topic väljs baserat på aktuell route
- [x] Routes utan mappning faller tillbaka på standardinnehåll
- [x] Drawern stängs med Escape-tangenten
- [x] Fokus-trap aktiveras när drawern är öppen

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
- [x] Innehållet byts utan att drawern stängs
- [x] Bakåt-knapp visas och fungerar efter navigering
- [x] `HelpService.open(topic)` öppnar korrekt topic direkt

---

# Tenancy (MT-kontexten)

---

# UC-MT001 – Skapa tenant (manuell, systemadmin)

## Sammanfattning
SystemAdmin skapar en ny tenant med subdomän och visningsnamn.

## Aktör
SystemAdmin

## Förutsättningar
- Multitenancy aktiverat
- SystemAdmin inloggad

## Flöde
1. SystemAdmin skickar `POST /system/tenants` med subdomän och visningsnamn
2. Systemet validerar att subdomänen är unik och följer format (`[a-z0-9-]+`)
3. `Tenant`-aggregat skapas med status `Active`
4. `TenantCreated`-event dispatkas
5. Systemet returnerar `TenantId`

## Affärsregler
- Subdomän får inte redan finnas
- Subdomän valideras mot regex `^[a-z0-9-]{3,63}$`
- Konvent skapas separat av den nya tenantens admin (ej i detta flöde)

## Acceptanskriterier
- [ ] Tenant skapas med unik subdomän och status `Active`
- [ ] Duplikat subdomän ger 422
- [ ] `TenantCreated`-event dispatkas
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-MT002 – Lös upp tenant från request

## Sammanfattning
Middleware identifierar aktuell tenant från request-subdomänen (produktion) eller `X-Tenant-ID`-header (development).

## Aktör
Systemet (middleware)

## Förutsättningar
- Request inkommer mot SaaS-deploy

## Flöde
1. Middleware extraherar host-header
2. I produktion: subdomän parsas ur host (`gammacon.conclave.se` → `gammacon`)
3. I development: `X-Tenant-ID`-header används som fallback
4. Tenant slås upp mot `Tenants`-tabell
5. `TenantId` sätts i `HttpContext.Items`
6. Suspended tenant returnerar 403 med `errorCode: tenant_suspended`
7. Okänd tenant returnerar 404

## Affärsregler
- `X-Tenant-ID`-header ignoreras i produktion
- Tenant med status `Suspended` ger 403, inte 404

## Acceptanskriterier
- [ ] Korrekt `TenantId` sätts för känd tenant
- [ ] `X-Tenant-ID`-header ignoreras i produktionsmiljö
- [ ] Suspended tenant ger 403
- [ ] Okänd tenant ger 404
- [ ] Enhetstester täcker prioritetsordning och edge cases

---

# UC-MT003 – Suspendera tenant

## Sammanfattning
SystemAdmin suspenderar en aktiv tenant. Efterföljande requests mot tenantens subdomän returnerar 403.

## Aktör
SystemAdmin

## Förutsättningar
- Tenant existerar med status `Active`

## Flöde
1. SystemAdmin skickar `PUT /system/tenants/{tenantId}/suspend`
2. `Tenant.Suspend()` anropas
3. `TenantSuspended`-event dispatkas
4. Efterföljande requests mot tenantens subdomän returnerar 403

## Affärsregler
- Aktiva sessioner avbryts inte omedelbart – JWT-tokens gäller till expiry
- Redan suspended tenant ger domain-rule violation

## Acceptanskriterier
- [ ] Tenant-status ändras till `Suspended`
- [ ] `TenantSuspended`-event dispatkas
- [ ] Redan suspended tenant ger 422
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-MT004 – Återaktivera tenant

## Sammanfattning
SystemAdmin återaktiverar en suspenderad tenant.

## Aktör
SystemAdmin

## Förutsättningar
- Tenant med status `Suspended`

## Flöde
1. SystemAdmin skickar `PUT /system/tenants/{tenantId}/restore`
2. `Tenant.Restore()` anropas
3. Tenant returnerar till `Active`

## Acceptanskriterier
- [ ] Tenant-status ändras tillbaka till `Active`
- [ ] Kommandohanterare har tillhörande enhetstest

---

# UC-MT005 – Registrera tenant-användare

## Sammanfattning
En tenant-admin registrerar en ny användare inom sin tenant. Samma e-post kan finnas hos olika tenants utan konflikt.

## Aktör
Tenant-admin

## Förutsättningar
- Tenant aktiv
- Inloggad som `ConventionAdministrator`

## Flöde
1. Admin skickar `POST /auth/register` med e-post och lösenord på tenant-subdomän
2. Middleware har redan resolvar `TenantId` från subdomänen
3. Systemet kontrollerar att e-post inte redan finns för denna tenant
4. `ApplicationUser` skapas med `UserType = TenantUser` och korrekt `TenantId`
5. `Person`-entitet skapas i Convention-BC med samma `TenantId`
6. `ApplicationUser.PersonId` kopplas till den nya `Person`

## Affärsregler
- Samma e-post kan registreras hos olika tenants utan konflikt
- Samma e-post hos samma tenant ger 422 med `errorCode: email_already_exists`
- `TenantId` tas aldrig från request-body – alltid från middleware

## Acceptanskriterier
- [ ] Användare skapas med korrekt `TenantId` och `UserType = TenantUser`
- [ ] Duplikat e-post inom samma tenant ger 422
- [ ] Samma e-post hos annan tenant tillåts
- [ ] `Person` skapas och kopplas till `ApplicationUser`

---

# UC-MT006 – Logga in som tenant-användare

## Sammanfattning
En tenant-användare loggar in via tenant-subdomänen. Systemadmin-konton kan aldrig autentiseras via denna endpoint.

## Aktör
Tenant-användare

## Förutsättningar
- Användare registrerad hos tenanten

## Flöde
1. Användare skickar `POST /auth/login` med e-post och lösenord på tenant-subdomän
2. `TenantAwareUserService.FindTenantUserAsync(email, tenantId)` anropas
3. Lösenord verifieras
4. JWT utfärdas med `tenant_id`, `user_type: tenant_user` och relevanta rollclaims
5. Token returneras

## Affärsregler
- Felaktigt lösenord ger 401 – aldrig information om huruvida e-posten finns
- En systemadmins e-post går inte att logga in med via tenant-endpointen
- Token innehåller alltid `tenant_id` för tenant-användare

## Acceptanskriterier
- [ ] Lyckad inloggning returnerar JWT med `tenant_id` och `user_type: tenant_user`
- [ ] Systemadmins e-post kan inte autentiseras via tenant-endpointen
- [ ] Felaktigt lösenord ger 401

---

# UC-MT007 – Logga in som systemadmin

## Sammanfattning
SystemAdmin loggar in via en separat endpoint utanför tenant-middleware-scopet.

## Aktör
SystemAdmin

## Förutsättningar
- SystemAdmin-användare skapad manuellt i databasen

## Flöde
1. Admin skickar `POST /system/auth/login` med e-post och lösenord
2. Endpointen ligger utanför `TenantResolutionMiddleware`s scope
3. `TenantAwareUserService.FindSystemAdminAsync(email)` anropas
4. JWT utfärdas med `user_type: system_admin` och `is_system_admin: true`
5. Token returneras

## Affärsregler
- Endpointen är inte nåbar via tenant-subdomän – endast via system-ingången
- En tenant-användares e-post går inte att logga in med via systemadmin-endpointen
- SystemAdmin-token innehåller aldrig `tenant_id`

## Acceptanskriterier
- [ ] Lyckad inloggning returnerar JWT med `user_type: system_admin`, utan `tenant_id`
- [ ] Tenant-användares e-post kan inte autentiseras via systemadmin-endpointen
- [ ] Endpointen är inte exponerad via tenant-subdomän

---

# UC-MT008 – Provisionera konvent för ny tenant

## Sammanfattning
SystemAdmin skapar ett konvent och en admin-användare åt en ny tenant. Flödet återanvänder `CreateConventionCommand`; `TenantSeedInterceptor` sätter `TenantId` automatiskt.

## Aktör
SystemAdmin (fas 1–3), Tenant-admin (fas 4 – self-service)

## Förutsättningar
- Tenant skapad (UC-MT001)

## Flöde
1. Aktör skickar `POST /conventions` med `TenantId` i JWT eller header
2. `Convention`-aggregat skapas med korrekt `TenantId` (satt av interceptorn)
3. En `Person` skapas och tilldelas rollen `ConventionAdministrator`
4. Returnerar `ConventionId`

## Affärsregler
- `TenantId` sätts av `TenantSeedInterceptor`, aldrig manuellt i handlern
- Flödet återanvänder befintlig `CreateConventionCommand`

## Acceptanskriterier
- [ ] `Convention` skapas med korrekt `TenantId`
- [ ] `ConventionAdministrator` skapas för tenant-admin
- [ ] `ConventionId` returneras

---


# UC-RC001 – Redigera eventbeskrivning med markdown

## Sammanfattning
Admin eller evenemangsarrangör redigerar ett evenemangs publika beskrivning i markdown-format. Den publika appen renderar texten som formaterad HTML.

## Aktör
Konventionsadministratör eller evenemangsarrangör (LeadOrganiser/CoOrganiser)

## Förutsättningar
- Evenemanget finns
- Utföraren är admin eller arrangör för det aktuella evenemanget

## Flöde
1. Aktören öppnar redigeringsformuläret för evenemanget
2. Systemet visar nuvarande beskrivning i en textarea med en live-förhandsvisning bredvid
3. Aktören redigerar texten i markdown-format
4. Aktören sparar formuläret
5. Systemet lagrar råtexten; publika appen renderar den som HTML vid visning

## Affärsregler
- Beskrivningen lagras alltid som råmarkdown, aldrig som HTML
- Max 10 000 tecken
- Rå HTML-taggar i beskrivningen ska inte renderas (saniteras vid visning)
- Arrangör kan bara redigera sin egen events beskrivning; admin kan redigera alla

## Domänhändelser
- Inga nya (använder befintlig `EditDescription`-metod)

## Acceptanskriterier
- [ ] Markdown renderas korrekt i publika appen (rubriker, fetstil, listor, länkar)
- [ ] Rå HTML-taggar saniteras och renderas inte
- [ ] Text över 10 000 tecken returnerar valideringsfel
- [ ] Arrangör kan inte redigera ett evenemang de inte tillhör
- [ ] Befintlig testtäckning för `EditDescription` validerar gränsvärden

---

# UC-RC002 – Ladda upp bild

## Sammanfattning
Admin eller evenemangsarrangör laddar upp en bildfil och får tillbaka en publik URL som kan bäddas in i markdown-innehåll.

## Aktör
Konventionsadministratör eller evenemangsarrangör

## Förutsättningar
- Utföraren är autentiserad

## Flöde
1. Aktören klickar "Ladda upp bild" i markdown-editorn
2. Aktören väljer en bildfil (JPEG, PNG, GIF, WebP)
3. Systemet validerar filtyp och storlek
4. Systemet sparar filen i tenant-scopad lagring
5. Systemet returnerar en publik URL
6. Editorn infogar `![bild](url)` vid markörpositionen i textytan

## Affärsregler
- Tillåtna format: JPEG, PNG, GIF, WebP
- Max filstorlek: konfigurerbar, standard 5 MB
- Filer sparas tenant-scopade; en tenants filer kan aldrig skrivas över av en annan
- URL:en är publik och tillgänglig utan autentisering

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Uppladdad bild är åtkomlig via returnerad URL utan autentisering
- [x] Bilder från ett tenant kan inte skrivas över av ett annat (filsökvägen inkluderar tenantId)
- [x] Ogiltig filtyp returnerar valideringsfel
- [x] Fil över maxstorlek returnerar valideringsfel
- [x] Markdown-syntaxen infogas korrekt vid markörpositionen i textarea

---

# UC-RC003 – Hantera informationssida

## Sammanfattning
En administratör skapar, redigerar, publicerar och tar bort redaktionella informationssidor. Sidor kan vara kopplade till hela konventionen eller till en specifik upplaga.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör

## Flöde – Skapa
1. Administratören anger titel, slug, scope (Konvention eller Upplaga), valfritt EditionId, och innehåll (markdown)
2. Systemet validerar att slug är unik inom valt scope
3. Systemet skapar sidan som opublicerad

## Flöde – Uppdatera
1. Administratören öppnar sidan och ändrar titel, slug eller innehåll
2. Systemet validerar slug-unikhet om slug ändrats
3. Systemet sparar uppdateringen

## Flöde – Publicera / Avpublicera
1. Administratören växlar publiceringsstatus
2. Systemet uppdaterar `IsPublished`

## Flöde – Radera
1. Administratören raderar sidan
2. Systemet tar bort posten permanent

## Affärsregler
- Slug måste vara unik per scope (Convention-scope eller Edition-scope)
- Slug får bara innehålla gemener, siffror och bindestreck
- Opublicerade sidor returnerar 404 i publika API:et
- En konventionsscopead sida (`EditionId = null`) är åtkomlig oavsett aktiv upplaga
- En upplagescopead sida är bara åtkomlig i kontexten av sin upplaga

## Domänhändelser
- `PagePublished { pageId, slug, occurredAt }`
- `PageUnpublished { pageId, slug, occurredAt }`

## Acceptanskriterier
- [x] Sida sparas med giltigt PageId
- [x] Slug-kollision inom samma scope returnerar valideringsfel
- [x] Ogiltigt slug-format returnerar valideringsfel
- [x] Opublicerad sida returnerar 404 i publika API:et
- [x] Konventionsscopead sida är åtkomlig utan EditionId
- [x] Upplagescopead sida kräver att rätt edition är aktiv i kontexten
- [x] Kommandohanterarna har tillhörande enhetstester

---

# UC-RC004 – Visa informationssida (publik)

## Sammanfattning
En besökare navigerar till en informationssida via slug. Systemet returnerar den publicerade sidan och frontenden renderar markdown-innehållet som HTML.

## Aktör
Besökare (anonym eller inloggad)

## Förutsättningar
- Sidan finns och är publicerad

## Flöde
1. Besökaren navigerar till `/pages/:slug`
2. Systemet söker efter publicerad sida med angiven slug
3. Systemet prövar upplagescopead sida för aktiv edition; om ej funnen prövas konventionsscopead sida med samma slug
4. Systemet returnerar sidans titel och markdown-innehåll
5. Frontenden renderar markdown som HTML

## Affärsregler
- Upplagescopead sida prioriteras om slug matchar i båda scopen
- Opublicerad eller saknad sida returnerar 404
- Ingen autentisering krävs

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Publicerad sida returneras korrekt med titel och innehåll
- [x] Opublicerad sida returnerar 404
- [x] Edition-scopad sida prioriteras framför konventionsscopead vid slug-kollision
- [x] Konventionsscopead sida är åtkomlig utan aktiv edition

---

# UC-RC005 – Redigera mailmall

## Sammanfattning
En administratör anpassar ämnesrad och brödtext för en specifik typ av systemmail. Anpassad mall används vid nästa utskick av den typen. En standardmall i källkoden finns alltid att återgå till.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör

## Flöde
1. Administratören öppnar malllistan och väljer en malltyp att redigera
2. Systemet visar nuvarande ämnesrad och brödtext (markdown med variabelplatshållare, t.ex. `{{firstName}}`)
3. Administratören redigerar ämne och/eller brödtext
4. Systemet sparar mallen och markerar den som anpassad (`IsCustomized = true`)

## Affärsregler
- Varje malltyp har en hårdkodad standardmall i källkoden
- En anpassad mall ersätter standardmallen vid utskick
- Variabelplatshållare har formen `{{variabelnamn}}`
- Okända variabelnamn ger inget fel – de ersätts med tom sträng vid rendering
- Brödtext lagras som rawmarkdown; systemet renderar till HTML vid utskick

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] Ämne och brödtext sparas per malltyp
- [ ] `IsCustomized` sätts till `true` vid sparad anpassning
- [ ] Anpassad mall används vid nästa utskick av rätt typ
- [ ] Variabelsubstitution fungerar korrekt (`{{firstName}}` → personens förnamn)
- [ ] Okänd variabel ersätts med tom sträng (inget undantag kastas)

---

# UC-RC006 – Återställ mailmall till standard

## Sammanfattning
En administratör återställer en anpassad mailmall till sin hårdkodade standardtext.

## Aktör
Konventionsadministratör

## Förutsättningar
- Malltypen finns och har `IsCustomized = true`

## Flöde
1. Administratören klickar "Återställ till standard" för en malltyp
2. Systemet ersätter lagrad mall med hårdkodad standardmall
3. Systemet markerar mallen som ej anpassad (`IsCustomized = false`)

## Affärsregler
- Standardmallarna är hårdkodade i källkoden och kan inte ändras utan redeploy
- Återställning kan inte ångras

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] `IsCustomized` sätts till `false`
- [ ] Nästa utskick av aktuell typ använder standardtextens ämne och brödtext
- [ ] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-EX001 – Exportera upplaga som JSON

## Sammanfattning
En administratör exporterar en upplaga som ett fristående JSON-dokument som kan användas för att skapa en ny upplaga med samma struktur.

## Aktör
Konventionsadministratör

## Förutsättningar
- Upplagan finns
- Utföraren är administratör för konventionen

## Flöde
1. Administratören navigerar till exportsidan i admin-appen
2. Administratören väljer vilka valbara block som skall ingå: evenemang och/eller biljetttyper
3. Admin-appen hämtar upplagan med vald data och producerar ett `EditionExportDocument`
4. Admin-appen visar dokumentet som formaterad JSON inline så att administratören kan kopiera innehållet
5. Administratören kan även ladda ner samma JSON som fil

## Valbara block
- **Evenemang** – titel, beskrivning, kategorinamn, registreringstyp, inpläggningsregler, schemaönskemålstext, godkänd medarrangörslimit (`coOrganiserLimit`), sessioner (lokal via namn, dag (relativt), klockslag, max-platser, starttyp)
- **Biljetttyper** – namn, pris, typ (Visitor/Organiser/Staff), beskrivning, giltiga dagar (relativt), tillåtna kategorier via namn
- **Bemanningspass** – exporteras alltid under respektive station: dag (relativt), klockslag, min/max bemanning och passansvarig via e-post

## Kategoribeskrivningar i dokumentet
- Kategorier exporterar både `organizerInstructions` (intern instruktion för arrangörer) och `publicDescription` (publik text).
- För bakåtkompatibilitet kan äldre dokument innehålla `description`; importen mappar då detta till `publicDescription` om `publicDescription` saknas.

## Datumrepresentation
Alla datum uttrycks relativt till upplagets startdatum. Dag 1 = första dagen. Klockslag är lokaltid som `HH:mm`.

## Vad som aldrig exporteras
- Interna ID:n (inga `Guid`-värden i dokumentet)
- Transaktionsdata: biljetter, registreringar, staffansökningar, kommentarer, medarrangörsstatus
- Upplagens status, öppna registreringar eller koordinatorer
- Identiteter (person-ID:n) – person-referenser uttrycks som e-postadresser

## Affärsregler
- Dokumentet är versions­märkt med `schemaVersion` för framtida kompabilitet
- Venues, staffområden, stationer, bemanningspass och kategorier exporteras alltid som en del av upplagestrukturen
- Stationer placeras hierarkiskt under sitt staffområde i dokumentet
- Bemanningspass placeras hierarkiskt under sin station i dokumentet
- Sessionens dag beräknas som `(sessionDatum - upplagets startdatum).TotalDays + 1`

## Implementationssteg
- [x] `R-EX01` Kontrakt: `EditionExportDocument` och DTO:er finns i application-lagret utan exportlogik
- [x] `R-EX02` Backend: kommando/handler/endpoint skapar och returnerar JSON-fil
- [x] `R-EX03` Admin-UI: exportsida med valbara block, inline JSON, kopiera och nedladdningsknapp

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Exportdokumentet innehåller inga interna ID:n
- [x] Datum är relativa (dag-nummer, inte datum)
- [x] Valfria block inkluderas respektive utelämnas korrekt baserat på administratörens val
- [x] Filen laddas ner med `Content-Disposition: attachment` och ett meningsfullt filnamn
- [x] Person-referenser exporteras som e-postadresser
- [x] Admin-appen visar JSON inline för kopiering
- [x] Admin-appen erbjuder nedladdning av samma JSON
- [x] Stationer och bemanningspass exporteras utan interna ID:n

---

# UC-EX002 – Importera upplaga från JSON

## Sammanfattning
En administratör skapar en ny upplaga genom att klistra in ett exportdokument och ange namn och startdatum för den nya upplagan. Systemet tolkar dokumentet, skapar strukturen och rapporterar eventuella avvikelser.

## Aktör
Konventionsadministratör

## Förutsättningar
- Konventionen finns
- Utföraren är administratör för konventionen
- Administratören har ett giltigt `EditionExportDocument` som JSON

## Flöde
1. Administratören klickar "Skapa ny upplaga" på dashboard
2. Administratören väljer alternativet "Importera från JSON"
3. Administratören klistrar in JSON-dokumentet i textfältet
4. Admin-appen tolkar dokumentet och visar en förhandsgranskning (antal venues, kategorier, evenemang m.m.)
5. Administratören anger namn och startdatum för den nya upplagan
6. Administratören klickar "Skapa från import"
7. Systemet skapar upplagan och all inkluderad struktur med nya interna ID:n
8. Systemet returnerar den nya `EditionId` och en lista med importvarningar
9. Admin-appen navigerar till den nya upplagan och visar varningarna

## Importlogik
Systemet utför skapandet i följande ordning:
1. Skapa upplagan med angivet namn och period (startdatum + `durationDays`)
2. Skapa schemaläggningsdagar (relativt till startdatum)
3. Skapa venues – bygg `namn → VenueId`-karta
4. Skapa staffområden – slå upp ansvarig via e-post; fallback till importerande person
5. Skapa stationer – slå upp staffområde via namnet
6. Skapa bemanningspass – slå upp station via staffområdets namn + stationsnamn; passansvarig via e-post, fallback till importerande person
7. Skapa kategorier – slå upp ansvarig via e-post; fallback till importerande person
   - `organizerInstructions` och `publicDescription` importeras om de finns
   - äldre `description` används som fallback för `publicDescription`
8. Skapa biljetttyper (om inkluderade) – slå upp `allowedCategoryNames` mot nya CategoryId:n
9. Skapa evenemang (om inkluderade):
   - Slå upp kategori via namn; evenemang utan matchande kategori hoppas över med varning
   - Skapas med status `Draft`, `LeadOrganiserId` = importerande person
   - `coOrganiserLimit` importeras till evenemangets godkända medarrangörsgräns
   - Sessioner: slå upp lokal via namn; session utan matchande lokal hoppas över med varning

## Datumrekonstruktion
- Upplagets period: `startDate` till `startDate + (durationDays - 1) dagar`
- Schemaläggningsdagar: `startDate + (day - 1) dagar`
- Sessionstidpunkter: `startDate + (day - 1) dagar` kombinerat med `startTime`/`endTime`

## Importvarningar
Systemet samlar alla avvikelser och returnerar dem efter att upplagan skapats. Importen avbryts inte av mjuka fel.

| Varningskod | Beskrivning |
|---|---|
| `PersonNotFound` | E-post finns inte bland konventionens members – ersatt av importerande person |
| `CategoryNotFound` | Evenemang refererade kategorinamn som inte matchade – evenemang hoppades över |
| `VenueNotFound` | Session refererade lokal­namn som inte matchade – session hoppades över |
| `EventSkipped` | Evenemang kunde inte skapas av annat skäl |

## Affärsregler
- Importen skapar alltid en ny upplaga – befintliga upplagor uppdateras aldrig
- Alla interna ID:n genereras på nytt
- Dokumentets `schemaVersion` valideras; okänd version avvisas med fel
- `durationDays` i dokumentet avgör upplagets längd; det angivna startdatumet styr förskjutningen
- Upplagan skapas alltid i status `Draft` oavsett källupplagets status

## Implementationssteg
- [x] `R-EX01` Kontrakt: importen utgår från samma `EditionExportDocument` som exportflödet
- [x] `R-EX04` Backend: importkommando/handler/endpoint skapar ny upplaga och returnerar varningar
- [x] `R-EX05` Admin-UI: importpanel, förhandsgranskning och varningsdialog

## Domänhändelser
- `EditionCreated` (för den nya upplagan)
- Domänhändelser per skapat barn (venues, kategorier etc.) om de höjs av respektive metod

## Acceptanskriterier
- [x] Ny upplaga skapas med korrekt namn, period och alla strukturella element
- [x] Datum rekonstrueras korrekt från relativa dag-nummer och angivet startdatum
- [x] Person-referenser löses upp via e-post; fallback till importerande person loggas som varning
- [x] Evenemang utan matchande kategori hoppas över och loggas som varning
- [x] Sessioner utan matchande lokal hoppas över och loggas som varning
- [x] Varningslistan visas för administratören efter avslutad import
- [x] Upplagan skapas med status `Draft`

---

# Planerade teamflöden

UC-TM001–UC-TM004 beskriver planerad funktionalitet för laganmälningar. De är inte implementerade i nuvarande domänmodell.

# UC-TM001 – Konfigurera laganmälning på evenemang

## Sammanfattning
Arrangören väljer om evenemanget tar individuella anmälningar eller laganmälningar. Vid laganmälning anger arrangören minsta och högsta tillåtna antal deltagare per lag.

## Aktör
Huvud- eller medarrangör

## Förutsättningar
- Evenemanget finns och har status Utkast
- Utföraren är huvud- eller medarrangör

## Flöde
1. Arrangören anger EventId, RegistrationMode (Individual | Team) och, om Team, MinTeamSize och MaxTeamSize
2. Systemet validerar lagstorlek om Team är valt
3. Systemet uppdaterar RegistrationMode (och eventuell TeamSize) på Event-aggregatet
4. Systemet sparar ändringen

## Affärsregler
- Konfiguration är bara möjlig när evenemanget är i Utkast-läge
- Om RegistrationMode är Individual ska TeamSize inte vara satt
- Om RegistrationMode är Team måste MinTeamSize och MaxTeamSize anges
- MinTeamSize måste vara ≥ 1
- MaxTeamSize måste vara ≥ MinTeamSize

## Domänhändelser
- Inga

## Implementationssteg
- [ ] `R-TM01` Nytt fält `RegistrationMode` (Individual | Team) + value object `TeamSize { Min, Max }` på `Event`-aggregatet; domänmetod `ConfigureTeamRegistration(mode, min, max)` med invarianter; enhetstest

## Acceptanskriterier
- [ ] RegistrationMode och TeamSize sparas korrekt på evenemanget
- [ ] Konfiguration på ett icke-Utkast-evenemang returnerar valideringsfel
- [ ] MaxTeamSize < MinTeamSize returnerar valideringsfel
- [ ] MinTeamSize < 1 returnerar valideringsfel
- [ ] RegistrationMode Individual med angiven TeamSize returnerar valideringsfel
- [ ] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-TM002 – Anmäl lag

## Sammanfattning
En person anmäler ett lag till ett evenemang med laganmälning. Personen anger lagnamnet, blir automatiskt lagkapten och erhåller en laganmälan med status Väntande.

## Aktör
Besökare (person registrerad i konventionen)

## Förutsättningar
- Evenemanget finns, är publicerat och har RegistrationMode = Team
- Besökarregistrering för upplagan är öppen
- Personen finns och tillhör konventionen
- Personen har ingen annan aktiv laganmälan för samma evenemang

## Flöde
1. Besökaren anger EventId, lagnamn och sitt PersonId
2. Systemet validerar att evenemanget accepterar laganmälningar
3. Systemet skapar ett Team-aggregat med lagnamnet och personen som captain
4. Systemet skapar en TeamEventRegistration med status Pending kopplad till teamet och evenemanget
5. Systemet returnerar TeamId och TeamEventRegistrationId

## Affärsregler
- Evenemanget måste ha RegistrationMode = Team
- Lagnamnet får inte vara tomt
- En person kan bara ha en aktiv laganmälan (status Pending eller Confirmed) per evenemang
- Lagmedlemmar (utöver captainen) behöver inte anges i fas 1

## Domänhändelser
- `TeamCreated { teamId, editionId, captainPersonId, name, occurredAt }`
- `TeamEventRegistrationCreated { registrationId, teamId, eventId, occurredAt }`

## Implementationssteg
- [ ] `R-TM02` `Team`-aggregat med captain och lagnamn; enhetstest
- [ ] `R-TM03` `TeamEventRegistration`-aggregat med livscykel Pending → Confirmed | Cancelled; enhetstest
- [ ] Kommando `RegisterTeamForEventCommand` + handler; validator; endpoint `POST /api/events/{eventId}/team-registrations`

## Acceptanskriterier
- [ ] Team och TeamEventRegistration skapas; captainPersonId = anmälande person
- [ ] Lagnamn anges och sparas korrekt
- [ ] Anmälan till ett evenemang med RegistrationMode = Individual returnerar valideringsfel
- [ ] Tom lagnamn returnerar valideringsfel
- [ ] Dubbel aktiv anmälan för samma person och evenemang returnerar valideringsfel
- [ ] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-TM003 – Bekräfta laganmälan

## Sammanfattning
En arrangör eller administratör bekräftar en väntande laganmälan. Laganmälans status ändras till Bekräftad.

## Aktör
Arrangör (huvud- eller medarrangör) eller konventionsadministratör

## Förutsättningar
- TeamEventRegistration finns med status Pending
- Utföraren är arrangör för evenemanget eller konventionsadministratör

## Flöde
1. Arrangören anger TeamEventRegistrationId
2. Systemet validerar status och behörighet
3. Systemet ändrar status till Confirmed
4. Systemet sparar ändringen

## Affärsregler
- Bara en Pending-anmälan kan bekräftas
- Utföraren måste vara arrangör för evenemanget eller administratör

## Domänhändelser
- `TeamEventRegistrationConfirmed { registrationId, teamId, eventId, occurredAt }`

## Implementationssteg
- [ ] `R-TM03` Domänmetod `Confirm()` på `TeamEventRegistration`; enhetstest
- [ ] Kommando `ConfirmTeamRegistrationCommand` + handler; endpoint `POST /api/team-registrations/{id}/confirm`

## Acceptanskriterier
- [ ] Status ändras till Confirmed
- [ ] Bekräftning av en icke-Pending-anmälan returnerar valideringsfel
- [ ] Saknad behörighet returnerar Forbidden
- [ ] Kommandohanteraren har ett tillhörande enhetstest

---

# UC-TM004 – Avboka laganmälan

## Sammanfattning
En lagkapten eller administratör avbokar en laganmälan. Laganmälans status sätts till Cancelled.

## Aktör
Lagkapten (personen som skapade laganmälan) eller konventionsadministratör

## Förutsättningar
- TeamEventRegistration finns med status Pending eller Confirmed
- Utföraren är captainen för laget eller konventionsadministratör

## Flöde
1. Utföraren anger TeamEventRegistrationId
2. Systemet validerar status och behörighet
3. Systemet ändrar status till Cancelled
4. Systemet sparar ändringen

## Affärsregler
- En Cancelled-anmälan kan inte avbokas igen
- Captainen kan avboka sin laganmälan oavsett om den är Pending eller Confirmed
- Administratör kan avboka vilken laganmälan som helst

## Domänhändelser
- `TeamEventRegistrationCancelled { registrationId, teamId, eventId, cancelledByPersonId, occurredAt }`

## Implementationssteg
- [ ] `R-TM03` Domänmetod `Cancel(cancelledByPersonId)` på `TeamEventRegistration`; enhetstest
- [ ] Kommando `CancelTeamRegistrationCommand` + handler; endpoint `POST /api/team-registrations/{id}/cancel`

## Acceptanskriterier
- [ ] Status ändras till Cancelled
- [ ] Avbokning av redan Cancelled-anmälan returnerar valideringsfel
- [ ] Saknad behörighet (varken captain eller admin) returnerar Forbidden
- [ ] Kommandohanteraren har ett tillhörande enhetstest
- [x] Kommandohanteraren har tillhörande enhetstester

---

# UC-RX001 – Tilldela receptionsroll

## Sammanfattning
En konventionsadministratör tilldelar en person rollen `ReceptionStaff` för en specifik `Edition`. Personen får därmed tillgång till receptionsappen och JWT-claim `is_reception`.

## Aktör
Konventionsadministratör

## Förutsättningar
- `Edition` finns
- `Person` finns och tillhör konventet
- Utförande användare är administratör för konventet

## Flöde
1. Administratören anger `EditionId` och `PersonId`
2. Systemet kontrollerar att personen inte redan har receptionsrollen för upplagan
3. Systemet skapar en `ReceptionStaff`-post på `Edition`
4. Systemet returnerar bekräftelse

## Affärsregler
- En person kan bara ha receptionsrollen en gång per `Edition`
- Konventionsadministratörer har implicit receptionsåtkomst utan att tilldelas rollen

## Domänhändelser
- `ReceptionStaffAdded { editionId, personId, addedById, occurredAt }`

## Acceptanskriterier
- [x] `ReceptionStaff`-post skapas och kopplas till korrekt `EditionId`
- [x] Dubblett ger valideringsfel
- [x] Saknad behörighet returnerar Forbidden
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-RX002 – Ta bort receptionsroll

## Sammanfattning
En konventionsadministratör tar bort en persons `ReceptionStaff`-roll från en `Edition`.

## Aktör
Konventionsadministratör

## Förutsättningar
- `ReceptionStaff`-post finns för angiven `EditionId` och `PersonId`
- Utförande användare är administratör för konventet

## Flöde
1. Administratören anger `EditionId` och `PersonId`
2. Systemet tar bort `ReceptionStaff`-posten
3. Systemet returnerar bekräftelse

## Affärsregler
- En person som är konventionsadministratör förlorar inte receptionsåtkomst om `ReceptionStaff`-posten tas bort

## Domänhändelser
- `ReceptionStaffRemoved { editionId, personId, removedById, occurredAt }`

## Acceptanskriterier
- [x] `ReceptionStaff`-post raderas
- [x] Borttagning av icke-existerande post ger valideringsfel
- [x] Saknad behörighet returnerar Forbidden
- [x] Kommandohanterare har tillhörande enhetstest

---

# UC-RX003 – Sök person vid receptionen

## Sammanfattning
Receptionspersonal söker efter en besökare, arrangör eller funktionär via namn, e-post eller biljett-ID. Resultatet används för att identifiera personen inför incheckning.

## Aktör
Receptionspersonal (konventionsadministratör eller `ReceptionStaff`)

## Förutsättningar
- Aktiv `Edition` finns
- Utförande användare har receptionsåtkomst

## Flöde
1. Personalen anger sökterm (fritext) eller skannat `TicketId`
2. Systemet söker mot `Person.Name`, `Person.Email` och `Ticket.TicketId` inom aktiv `Edition`
3. Systemet returnerar matchande personer med biljettstatussammanfattning

## Affärsregler
- Fritextsökning kräver minst 2 tecken
- Sökning på exakt `TicketId` returnerar direkt utan minimigräns
- Resultatlistan begränsas till 20 träffar

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Sökning på namn eller e-post returnerar matchande personer inom aktiv edition
- [x] Sökning på exakt `TicketId` returnerar rätt person direkt
- [x] Fritext kortare än 2 tecken ger valideringsfel
- [x] Receptionspersonal utan rätt roll får 403

---

# UC-RX004 – Visa personens biljetter vid incheckning

## Sammanfattning
Receptionspersonal hämtar alla biljetter en person har för aktiv `Edition`, med status och förmåner, som underlag för incheckning och biljettutdelning.

## Aktör
Receptionspersonal (konventionsadministratör eller `ReceptionStaff`)

## Förutsättningar
- `Person` finns
- Utförande användare har receptionsåtkomst

## Flöde
1. Personalen väljer en person (t.ex. via UC-RX003)
2. Systemet hämtar alla `Ticket`-poster för personen och aktiv `Edition`
3. Systemet returnerar biljetter med typ, status, giltighetsdagar, tillåtna kategorier och förmåner

## Affärsregler
- Biljetter visas oavsett status (inkl. `Revoked`) för att ge full bild
- `Collected`-biljetter markeras tydligt för att undvika dubbelutdelning

## Domänhändelser
- Inga

## Acceptanskriterier
- [x] Alla biljetter för person och aktiv edition returneras
- [x] Svaret inkluderar `TicketType.Name`, status, `ValidDays`, `AllowedCategories` och lista av `TicketPerk`
- [x] Receptionspersonal utan rätt roll får 403

---

# UC-RX005 – Walk-up-incheckning

## Sammanfattning
En person som inte registrerat sig i förväg anländer vid receptionen. Receptionspersonal skapar konto, reserverar biljetttyp, registrerar manuell betalning och checkar in personen i ett sammanhängande flöde.

## Aktör
Receptionspersonal (konventionsadministratör eller `ReceptionStaff`)

## Förutsättningar
- Aktiv `Edition` finns med minst en tillgänglig `TicketType`

## Flöde
1. Personalen söker efter personen och får ingen träff (UC-RX003)
2. Personalen anger namn och e-post och väljer biljetttyp
3. Systemet identifierar eller skapar personkonto (UC002)
4. Systemet skapar `VisitorRegistration` och `Ticket` med status `Reserved` (UC-VR001)
5. Personalen bekräftar betalning (kontant eller Swish)
6. Systemet registrerar manuell betalning → biljettstatus `Paid` (UC-TK004)
7. Systemet checkar in biljetten → biljettstatus `Collected` (UC-TK008)
8. Systemet visar förmånslistan för utdelning

## Affärsregler
- Om personen redan finns (e-postmatch) används det befintliga kontot
- Betalningsintegrering (Swish, kortläsare) är utanför scope i fas 1 – betalning registreras manuellt

## Domänhändelser
- Se UC002, UC-VR001, UC-TK004 och UC-TK008

## Implementationssteg
- [ ] `R-RX05` Frontend walk-up-komponent som orkestrerar UC002 → UC-VR001 → UC-TK004 → UC-TK008 i sekvens

## Acceptanskriterier
- [ ] Person som inte finns skapas med korrekt konventions-scope
- [ ] Person som redan finns (e-postmatch) återanvänds utan duplikat
- [ ] Biljett skapas, betalas och checkas in utan manuella mellansteg
- [ ] Förmåner visas i sista steget

---

# UC-CMS001 – Redigera upplagens innehållsinställningar

## Sammanfattning
En administratör redigerar de texter och rubriker som visas på den publika startsidan för en upplaga: hero-rubrik, ingress och uppmaningstexter för besökar-, arrangörs- och funktionärsregistrering. Värdena lagras som nyckel-värde-par i en `EditionContent`-entitet och ersätter hårdkodade UI-texter.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör för konventet
- Upplagan finns

## Flöde
1. Administratören öppnar "Innehållsinställningar" under upplagens inställningar i admin
2. Systemet visar ett formulär med samtliga definierade nycklar och deras nuvarande värden
3. Administratören redigerar ett eller flera värden
4. Systemet sparar ändringarna per nyckel
5. Publika appen hämtar värdena vid nästa sidladdning och renderar dem

## Affärsregler
- Nycklar är fördefinierade av systemet (t.ex. `hero.title`, `hero.ingress`, `cta.visitor.label`, `cta.organiser.label`, `cta.staff.label`)
- Om ett värde saknas i databasen används en hårdkodad fallback-text i publika appen
- Max 500 tecken per värde
- Tomma strängar behandlas som "ej angivet" och utlöser fallback

## Domänhändelser
- `EditionContentUpdated { editionId, key, occurredAt }`

## Implementationssteg
- [x] `R-CMS01` `EditionContent`-entitet (EditionId, Key, Value) under `Edition`-aggregatet; EF Core-konfiguration
- [x] Kommando `SetEditionContentCommand(editionId, items[])` + handler; endpoint `PUT /api/editions/{id}/content`
- [x] Query `GetEditionContentQuery` → `EditionContentDto[]`; endpoint `GET /api/editions/{id}/content` (anonym)
- [x] Admin-komponent "Innehållsinställningar" med formulär per definierad nyckel
- [x] Publik home-komponent läser in `EditionContent` via `effect()` och substituerar nycklarna med fallback

## Acceptanskriterier
- [x] Värde sparas och returneras korrekt per nyckel
- [x] Tom sträng behandlas som saknat värde; publika appen visar fallback
- [ ] Värde över 500 tecken returnerar valideringsfel (klientvalidering finns; backend-validering saknas ännu)
- [x] Saknad behörighet returnerar Forbidden
- [x] Kommandohanteraren har tillhörande enhetstest

---

# UC-CMS002 – Markera evenemang som utvalda

## Sammanfattning
En administratör markerar ett eller flera evenemang som "utvalda" för en upplaga. Publika startsidan visar de utvalda evenemangen i stället för ett automatiskt urval.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör
- Evenemanget finns och tillhör aktiv upplaga

## Flöde
1. Administratören öppnar eventlistan i admin
2. Administratören växlar "Utvalt"-flaggan på ett evenemang
3. Systemet sätter `IsFeatured = true/false` och ett `FeaturedSortOrder` (ordningstal inom utvalda)
4. Publika startsidan hämtar utvalda evenemang via `GET /api/events/featured`

## Affärsregler
- Max 6 utvalda evenemang per upplaga
- Om inga evenemang är markerade som utvalda visas de tre senast publicerade i publika appen (bakåtkompatibelt beteende)
- Bara publicerade evenemang visas på startsidan oavsett flagga

## Domänhändelser
- Inga nya (fältändring på `Event`)

## Implementationssteg
- [ ] `R-CMS02` Fält `IsFeatured` och `FeaturedSortOrder` på `Event`; EF Core-migration
- [ ] Admin-UI: toggle och drag-och-sortering i eventlistan
- [ ] Query `GetFeaturedEventsQuery`; endpoint `GET /api/events/featured` (anonym)
- [ ] Publik startsida konsumerar `/api/events/featured`

## Acceptanskriterier
- [ ] Max 6 utvalda evenemang; sjunde ger valideringsfel
- [ ] Opublicerat evenemang visas aldrig på startsidan trots `IsFeatured = true`
- [ ] Utan utvalda evenemang visas de tre senast publicerade
- [ ] Saknad behörighet vid toggle returnerar Forbidden

---

# UC-CMS003 – Ordna navigationsmenyn

## Sammanfattning
En administratör styr i vilken ordning sidor visas i den publika navigationens meny. Ordningen sparas på sidan och respekteras av den publika appen.

## Aktör
Konventionsadministratör

## Förutsättningar
- Minst en sida med `ShowInPublicMenu = true` finns

## Flöde
1. Administratören öppnar sidlistan i admin
2. Administratören anger ett `MenuSortOrder`-värde (heltal) per sida, alternativt drar om ordningen i ett drag-och-släpp-gränssnitt
3. Systemet sparar ordningsvärdena
4. Publika appens navigation renderar sidor med `ShowInPublicMenu = true` sorterade stigande på `MenuSortOrder`

## Affärsregler
- `MenuSortOrder` är ett icke-negativt heltal
- Sidor med samma ordningstal sorteras alfabetiskt på titel som tiebreaker
- Sidor med `ShowInPublicMenu = false` ignoreras oavsett ordningstal

## Domänhändelser
- Inga

## Implementationssteg
- [ ] `R-CMS03` Fält `MenuSortOrder` (int, default 0) på `Page`; EF Core-migration
- [ ] Kommando `UpdatePageMenuOrderCommand`; endpoint `PATCH /api/pages/{id}/menu-order`
- [ ] Admin-UI: ordningsfält i sidlistan
- [ ] Publik navigationsfråga sorterar på `MenuSortOrder ASC, Title ASC`

## Acceptanskriterier
- [ ] Sidor visas i rätt ordning i publika navigationen
- [ ] Sida med `ShowInPublicMenu = false` syns aldrig i menyn oavsett ordningstal
- [ ] Negativt ordningstal returnerar valideringsfel

---

# UC-CMS004 – Visa startsidan med admin-styrt innehåll

## Sammanfattning
En besökare öppnar den publika startsidan. Systemet kombinerar admin-konfigurerade texter, utvalda evenemang och den sorterade navigationsmenyn till en sammanhängande vy utan hårdkodade texter i klientkoden.

## Aktör
Besökare (anonym eller inloggad)

## Förutsättningar
- En aktiv upplaga finns

## Flöde
1. Besökaren navigerar till startsidan
2. Publika appen hämtar parallellt: `EditionContent`, utvalda evenemang (`/api/events/featured`) och publika menysidor (`/api/pages/menu`)
3. Hero-sektionen renderas med `hero.title` och `hero.ingress` från `EditionContent`; fallback till konventionsnamn och datum om saknas
4. CTA-korten renderas med texter från `EditionContent` och visas baserat på registreringsstatus
5. Utvalda evenemang visas i programsektionen

## Affärsregler
- Ingen autentisering krävs
- Fallback-texter i klientkoden används om `EditionContent`-nycklar saknas

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] Admin-konfigurerad hero-rubrik visas på startsidan
- [ ] Fallback-rubrik visas om nyckeln saknas i databasen
- [ ] CTA-kort för stängd registrering döljs korrekt oavsett textinställning
- [ ] Utvalda evenemang visas; automatiskt urval används om inga är markerade

---

# UC-BR001 – Konfigurera varumärket för ett konvent

## Sammanfattning
En administratör konfigurerar det visuella varumärket för ett konvent: primärfärg, accentfärg, logotyp och typsnitt. Inställningarna sparas i en `ConventionBranding`-entitet och tillämpas dynamiskt i den publika appen utan redeploy.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör för konventet

## Flöde
1. Administratören öppnar "Varumärke" under konventsinställningar i admin
2. Systemet visar nuvarande inställningar med förhandsvisning
3. Administratören redigerar färger (hex-format), laddar upp logotyp och väljer typsnitt ur en begränsad lista
4. Administratören sparar
5. Systemet lagrar `ConventionBranding` och returnerar en publik endpoint för branding-data
6. Publika appen hämtar ny branding vid nästa laddning

## Affärsregler
- Färger anges som hex-strängar (`#rrggbb`); ogiltigt format returnerar valideringsfel
- Logotyp: JPEG, PNG, SVG, WebP; max 1 MB; sparas via `IFileStorage` tenant-scopat
- Tillåtna typsnitt: `Inter`, `Roboto`, `Open Sans`, `Lato`, `Merriweather` (listan är utökningsbar i konfiguration)
- `CustomCss`: valfritt fritext-fält, max 5 000 tecken; renderas aldrig som rå `<style>`-tagg utan stansas in strikt som CSS-variabelöverskridning
- En `ConventionBranding`-post skapas on-demand om ingen finns (upsert-semantik)
- Branding är konventionsscoped, inte editionscoped – ett konvent har ett varumärke

## Domänhändelser
- `ConventionBrandingUpdated { conventionId, occurredAt }`

## Implementationssteg
- [ ] `R-BR01` `ConventionBranding`-entitet (ConventionId, PrimaryColor, AccentColor, LogoUrl, FaviconUrl, FontFamily, CustomCss); EF Core-konfiguration; migration
- [ ] Kommando `SetConventionBrandingCommand` + handler (upsert); endpoint `PUT /api/conventions/{id}/branding`
- [ ] Publik query `GetConventionBrandingQuery`; endpoint `GET /api/conventions/{id}/branding` (anonym, cacheable)
- [ ] Admin-komponent "Varumärke" med färgväljare, filuppladdning för logotyp och typsnittslista
- [ ] Admin-förhandsvisning: live mock av publika appens header med valda inställningar

## Acceptanskriterier
- [ ] Branding sparas och returneras korrekt
- [ ] Ogiltig hex-sträng returnerar valideringsfel
- [ ] Logotyp utanför tillåten typ eller storlek returnerar valideringsfel
- [ ] Otillåtet typsnitt returnerar valideringsfel
- [ ] Saknad behörighet returnerar Forbidden
- [ ] Kommandohanteraren har tillhörande enhetstest

---

# UC-BR002 – Visa publik app med konventionsvarumärke

## Sammanfattning
En besökare öppnar den publika appen. Appen hämtar konventionens `ConventionBranding` och applicerar färger, typsnitt och logotyp dynamiskt via CSS-variabler utan att sidan behöver byggas om.

## Aktör
Besökare (anonym eller inloggad)

## Förutsättningar
- En `ConventionBranding`-post finns för konventet (skapas med systemdefinierade defaultvärden om administratören inte konfigurerat något)

## Flöde
1. Publika appens shell-komponent hämtar `GET /api/conventions/{id}/branding` vid initialisering
2. Appen infogar CSS-variabler (`--brand-primary`, `--brand-accent`, `--brand-font-family` m.fl.) på `document.documentElement` via `style.setProperty`
3. Logotyp-URL sätts i navbar
4. Sidan renderas med konventionens färger och typsnitt

## Affärsregler
- Om branding-anropet misslyckas används systemdefinierade CSS-fallbacks (bakåtkompatibelt)
- CSS-variablerna sätts i realtid; inga `<style>`-block genereras med API-data
- HTTP-svaret för branding-endpointen inkluderar `Cache-Control: max-age=300` för att undvika onödiga anrop

## Domänhändelser
- Inga

## Acceptanskriterier
- [ ] Publika appen visar korrekt primärfärg och accentfärg från databasen
- [ ] Logotyp visas i navbar
- [ ] Om branding-anropet returnerar 404 används systemfallbacks utan synliga fel
- [ ] HTTP-svar inkluderar korrekt `Cache-Control`-header

---

# UC-I18N001 – Konfigurera tillgängliga språk för en upplaga

## Sammanfattning
En administratör anger vilket primärspråk en upplaga har och vilka ytterligare språk innehållet kan översättas till. Inställningen styr vilka locale-alternativ som visas i redaktörsgränssnitten och i publika appens språkväljare.

## Aktör
Konventionsadministratör

## Förutsättningar
- Utföraren är administratör
- Upplagan finns

## Flöde
1. Administratören öppnar upplagens grundinställningar
2. Administratören väljer primärspråk (t.ex. `sv`) ur en fast lista av stödda locales
3. Administratören aktiverar ytterligare språk (t.ex. `en`)
4. Systemet sparar `EditionLocale`-poster (EditionId, Locale, IsPrimary)
5. Redaktörsgränssnitt för sidor och evenemang visar nu fliklayout per aktiverat språk

## Affärsregler
- Exakt ett primärspråk per upplaga
- Stödda locales i fas 1: `sv`, `en`
- Primärspråket kan inte avaktiveras om det finns publicerat innehåll
- Att lägga till ett språk skapar inga översättningar automatiskt

## Domänhändelser
- `EditionLocaleAdded { editionId, locale, occurredAt }`
- `EditionLocalePrimaryChanged { editionId, locale, occurredAt }`

## Implementationssteg
- [ ] `R-I18N02` `EditionLocale`-entitet (EditionId, Locale, IsPrimary); EF Core-konfiguration; migration
- [ ] Kommando `SetEditionLocalesCommand` + handler; endpoint `PUT /api/editions/{id}/locales`
- [ ] Admin-UI: språkinställning i upplagens grundformulär

## Acceptanskriterier
- [ ] Exakt ett primärspråk sparas per upplaga; försök att sätta fler ger valideringsfel
- [ ] Okänd locale returnerar valideringsfel
- [ ] Borttagning av primärspråk returnerar valideringsfel
- [ ] Kommandohanteraren har tillhörande enhetstest

---

# UC-I18N002 – Redigera översättning av informationssida

## Sammanfattning
En administratör lägger till eller redigerar en översättning av en informationssida (titel + markdown-innehåll) för ett aktiverat språk. Originalsidan (primärspråket) ändras inte.

## Aktör
Konventionsadministratör

## Förutsättningar
- Sidan finns
- Mållocalen är aktiverad för upplagan (UC-I18N001)

## Flöde
1. Administratören öppnar sidan i admin
2. Systemet visar en flik per aktiverat språk; primärspråksfliken visar originalinnehållet (skrivskyddat)
3. Administratören väljer en översättningsflik och redigerar titel och innehåll
4. Systemet sparar `PageTranslation`-posten (PageId, Locale, Title, Content)

## Affärsregler
- En `PageTranslation` per (PageId, Locale)-kombination; upsert-semantik
- Primärspråkets innehåll ändras aldrig via översättningsflödet
- Publiceringsstatusen styrs av originalsidan; en oversättning kan inte publiceras separat

## Domänhändelser
- Inga

## Implementationssteg
- [ ] `R-I18N03` `PageTranslation`-entitet (PageId, Locale, Title, Content); EF Core-konfiguration; migration
- [ ] Kommando `SetPageTranslationCommand` + handler; endpoint `PUT /api/pages/{id}/translations/{locale}`
- [ ] Query `GetPageBySlugQuery` utökas med locale-parameter; returnerar rätt `PageTranslation` om tillgänglig
- [ ] Admin-UI: flikbaserat redigeringsformulär per locale

## Acceptanskriterier
- [ ] Översättning sparas och kan hämtas per (slug, locale)
- [ ] Primärspråket påverkas inte av översättningsoperationen
- [ ] Otillåten locale returnerar valideringsfel
- [ ] Kommandohanteraren har tillhörande enhetstest

---

# UC-I18N003 – Redigera översättning av evenemangsbeskrivning

## Sammanfattning
En arrangör eller administratör lägger till eller redigerar en översättning av ett evenemangs titel och beskrivning för ett aktiverat språk.

## Aktör
Konventionsadministratör eller evenemangsarrangör (LeadOrganiser/CoOrganiser)

## Förutsättningar
- Evenemanget finns
- Utföraren är admin eller arrangör för evenemanget
- Mållocalen är aktiverad för upplagan

## Flöde
1. Arrangören öppnar evenemangsformuläret i admin eller i arrangörsgränssnittet
2. Systemet visar en flik per aktiverat språk; primärspråket är skrivskyddat i detta flöde
3. Arrangören redigerar titel och beskrivning på målspråket
4. Systemet sparar `EventTranslation`-posten (EventId, Locale, Title, Description)

## Affärsregler
- En `EventTranslation` per (EventId, Locale)-kombination; upsert-semantik
- Max 10 000 tecken på Description per locale (samma gräns som originalet)
- Arrangör kan bara redigera översättningar för sina egna evenemang

## Domänhändelser
- Inga

## Implementationssteg
- [ ] `R-I18N04` `EventTranslation`-entitet (EventId, Locale, Title, Description); EF Core-konfiguration; migration
- [ ] Kommando `SetEventTranslationCommand` + handler; endpoint `PUT /api/events/{id}/translations/{locale}`
- [ ] Query `GetEventQuery` och `ListEventsQuery` utökas med locale-parameter
- [ ] Admin- och arrangörsgränssnitt: flikbaserat redigeringsformulär per locale

## Acceptanskriterier
- [ ] Översättning sparas och returneras per (eventId, locale)
- [ ] Arrangör kan inte redigera ett evenemang de inte tillhör
- [ ] Description över 10 000 tecken returnerar valideringsfel
- [ ] Kommandohanteraren har tillhörande enhetstest

---

# UC-I18N004 – Visa innehåll på valt språk

## Sammanfattning
En besökare väljer ett språk i den publika appen (eller systemet läser `Accept-Language`). API:t returnerar översatt titel och innehåll om en översättning finns; annars faller det tillbaka på primärspråket.

## Aktör
Besökare (anonym eller inloggad)

## Förutsättningar
- Mållocalen är aktiverad för upplagan

## Flöde
1. Besökaren väljer språk i publika appens språkväljare, eller webbläsarens `Accept-Language` läses vid första besök
2. Publika appen lägger till `?locale=en` (eller liknande) på API-anrop
3. API:t hämtar `PageTranslation` eller `EventTranslation` för angiven locale
4. Om ingen översättning finns returneras originaltexten (primärspråket) utan felstatus
5. Sidrubriker och innehåll renderas på valt språk

## Affärsregler
- Fallback till primärspråk är tyst – inga felmeddelanden visas
- Localepreferens sparas i `localStorage` och används vid nästa besök
- Systemtexter (knappar, etiketter) hanteras separat via Angular `@angular/localize` (UC hör till R-I18N01)

## Domänhändelser
- Inga

## Implementationssteg
- [ ] `R-I18N05` Publik app: språkväljare-komponent + locale-state i signal-service; locale skickas som query-parameter
- [ ] Samtliga publika queries som hämtar sidor och evenemang tar emot `locale`-parameter och tillämpar fallback-logik
- [ ] `Accept-Language`-header läses som fallback om ingen explicit locale anges

## Acceptanskriterier
- [ ] Sida med översättning returneras på valt språk
- [ ] Sida utan översättning returneras på primärspråket utan fel
- [ ] Localepreferens bevaras mellan sidladdningar
- [ ] Språkväljaren visar bara aktiverade locales för upplagan
