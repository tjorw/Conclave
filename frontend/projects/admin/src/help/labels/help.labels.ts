export type HelpTooltipKey =
  | 'convention.name'
  | 'convention.slug'
  | 'edition.name'
  | 'edition.startDate'
  | 'edition.endDate'
  | 'edition.staffCoordinator'
  | 'edition.eventCoordinator'
  | 'edition.scheduleDayTimes'
  | 'edition.publish'
  | 'edition.activeEdition'
  | 'edition.registrationWindows'
  | 'edition.importJson'
  | 'event.title'
  | 'event.scheduleRequests'
  | 'event.registrationType'
  | 'event.dropInRules'
  | 'event.coOrganisers'
  | 'registration.ticketPrice'
  | 'registration.ticketCategory'
  | 'registration.ticketValidDays'
  | 'registration.ticketAllowedCategories'
  | 'registration.promotionDiscount'
  | 'registration.promotionValidity'
  | 'registration.promotionAllowedTickets'
  | 'staff.station'
  | 'staff.shiftResponsible'
  | 'staff.shiftCapacity'
  | 'staff.assignment';

export const HELP_TOOLTIP_LABELS: Record<HelpTooltipKey, string> = {
  'convention.name': 'Konventets namn visas i admin och publika vyer.',
  'convention.slug': 'Sluggen identifierar konventet i URL:er och tenant-koppling.',
  'edition.name': 'Upplagans namn visas för deltagare, arrangörer och personal.',
  'edition.startDate': 'Första datumet som ingår i upplagan.',
  'edition.endDate': 'Sista datumet som ingår i upplagan.',
  'edition.staffCoordinator': 'Huvudansvarig för funktionärer och bemanning.',
  'edition.eventCoordinator': 'Huvudansvarig för program, arrangörer och granskning.',
  'edition.scheduleDayTimes': 'Standardtider per dag används som stöd i schemaflöden.',
  'edition.publish': 'Publicering gör upplagan synlig för publika flöden.',
  'edition.activeEdition': 'Aktiv upplaga är standardvalet för admin- och publikflöden.',
  'edition.registrationWindows': 'Styr vilka grupper som kan registrera sig just nu.',
  'edition.importJson': 'Skapar en ny upplaga från ett tidigare exporterat JSON-dokument.',
  'event.title': 'Publik titel för programpunkten. Håll den kort och tydlig.',
  'event.scheduleRequests': 'Arrangörens önskemål om tid, lokal eller praktiska behov.',
  'event.registrationType': 'Avgör om deltagare bokar plats, droppar in eller inte anmäler sig.',
  'event.dropInRules': 'Kort instruktion för hur drop-in fungerar för deltagare.',
  'event.coOrganisers': 'Antal extra arrangörsplatser som kan kopplas till evenemanget.',
  'registration.ticketPrice': 'Pris anges i kronor och används när besökaren köper biljetten.',
  'registration.ticketCategory': 'Styr om biljetten gäller besökare, arrangörer eller funktionärer.',
  'registration.ticketValidDays': 'Begränsar vilka upplagedagar biljetten gäller.',
  'registration.ticketAllowedCategories': 'Begränsar vilka programkategorier biljetten kan användas till.',
  'registration.promotionDiscount': 'Rabattens typ och värde beräknas vid inlösen av koden.',
  'registration.promotionValidity': 'Tidsintervall då kampanjkoden kan lösas in.',
  'registration.promotionAllowedTickets': 'Begränsar koden till vissa biljettyper. Tomt betyder alla.',
  'staff.station': 'Stationer är konkreta uppgifter eller platser inom ett funktionsområde.',
  'staff.shiftResponsible': 'Ansvarig är personen som leder eller följer upp passet.',
  'staff.shiftCapacity': 'Min och max anger hur många funktionärer passet behöver.',
  'staff.assignment': 'Tilldelning kopplar en funktionär till ett pass och kan bekräftas eller avböjas.',
};

export type HelpPanelKey =
  | 'edition.venues'
  | 'edition.categories'
  | 'event.workflow'
  | 'registration.visitors'
  | 'registration.promotionCodes'
  | 'person.registry';

export interface HelpPanelLabel {
  title: string;
  body: string;
}

export const HELP_PANEL_LABELS: Record<HelpPanelKey, HelpPanelLabel> = {
  'edition.venues': {
    title: 'Vad är lokaler?',
    body: 'Lokaler är platser där programpunkter och andra aktiviteter kan schemaläggas. De hör till en upplaga och kan återanvändas av flera evenemangssessioner.',
  },
  'edition.categories': {
    title: 'Vad är kategorier?',
    body: 'Kategorier samlar programpunkter inom samma område. De kan ha publika beskrivningar, arrangörsinstruktioner och en ansvarig person för granskning.',
  },
  'event.workflow': {
    title: 'Vad är evenemangsflödet?',
    body: 'Evenemang beskriver programpunkter medan sessioner placerar dem i tid och lokal. Listan hjälper administratörer att granska, följa status och hitta vad som behöver åtgärdas.',
  },
  'registration.visitors': {
    title: 'Vad är besökarregistreringar?',
    body: 'Besökarregistreringar visar biljetter, betalstatus och registreringstid för aktiv upplaga. De påverkas av biljettyper och om besökarregistreringen är öppen.',
  },
  'registration.promotionCodes': {
    title: 'Vad är kampanjkoder?',
    body: 'Kampanjkoder används för rabatter och särskilda erbjudanden på biljetter. Här följer du giltighet, inlösen och vilka biljetter koden gäller för.',
  },
  'person.registry': {
    title: 'Vad är personregistret?',
    body: 'Personregistret samlar personer i konventet. En person kan ha olika roller per upplaga, till exempel besökare, arrangör, funktionär eller administratör.',
  },
};
