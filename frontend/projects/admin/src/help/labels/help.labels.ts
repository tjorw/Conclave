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
  | 'edition.importJson';

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
};
