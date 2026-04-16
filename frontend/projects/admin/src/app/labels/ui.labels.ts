/** Gemensamma handlingsetiketter för knappar och formulär. */
export const ACTION = {
  save:        'Spara',
  saveChanges: 'Spara ändringar',
  cancel:      'Avbryt',
  create:      'Skapa',
  add:         'Lägg till',
  delete:      'Ta bort',
  edit:        'Redigera',
  accept:      'Acceptera',
  reject:      'Avslå',
  publish:     'Publicera',
  deactivate:  'Avaktivera',
  reactivate:  'Återaktivera',
  logout:      'Logga ut',
  assign:      'Tilldela',
  confirm:     'Bekräfta',
  send:        'Skicka',
  open:          'Öppna',
  change:        'Byt',
  schedule:      'Schemalägg',
  close:         'Stäng',
  sendRejection: 'Skicka avvisning',
  markHandled:   'Markera behandlad och svara',
} as const;

/** Tooltip-texter för ikonknappar. */
export const TOOLTIP = {
  save:          'Spara',
  cancel:        'Avbryt',
  edit:          'Redigera',
  delete:        'Ta bort',
  open:          'Öppna',
  accept:        'Acceptera',
  reject:        'Avslå',
  confirm:       'Bekräfta',
  unassign:      'Avboka',
  deactivate:    'Avaktivera',
  reactivate:    'Återaktivera',
  inactivate:    'Inaktivera',
  sendResetLink: 'Skicka återställningslänk',
  lockAccount:   'Lås konto',
  unlockAccount: 'Lås upp konto',
  makeAdmin:     'Gör till admin',
  removeAdmin:   'Ta bort admin',
  removeSelfAdmin: 'Du kan inte ta bort dig själv som admin',
  copyUrl:       'Kopiera URL',
  openInNewTab:  'Öppna i ny flik',
  hideTimeline:  'Dölj tidslinje',
  showTimeline:  'Visa tidslinje',
} as const;

/** Formulärfältsetiketter (mat-label). */
export const FIELD = {
  name:             'Namn',
  email:            'E-post',
  emailAddress:     'E-postadress',
  phone:            'Telefon',
  description:      'Beskrivning',
  title:            'Titel',
  event:            'Evenemang',
  startDate:        'Startdatum',
  endDate:          'Slutdatum',
  note:             'Anteckning (valfritt)',
  comment:          'Kommentar',
  category:         'Kategori',
  venue:            'Lokal',
  staffCoord:       'Staffkoordinator',
  eventCoord:       'Evenemangskoordinator',
  leadOrganiser:    'Huvudarrangör',
  registrationType: 'Registreringstyp',
  dropInRules:      'Drop-in-regler',
  duration:         'Längd (min)',
  seats:            'Platser',
  maxSeats:         'Max platser',
  startType:        'Starttyp',
  password:         'Lösenord',
} as const;

/** Platshållartexter i inputfält. */
export const PLACEHOLDER = {
  searchNameEmail:      'Sök namn eller e-post…',
  searchNameEventTitle: 'Sök namn eller evenemangstitel…',
  searchFunctionPerson: 'Sök funktion eller personnamn…',
  fullName:             'För- och efternamn',
  emailExample:         'namn@example.com',
  emailExampleSv:       'namn@exempel.se',
  phone:                '+46 70 123 45 67',
  nameExample:          'Förnamn Efternamn',
  editionName:          't.ex. Conclave 2026',
  optional:             'Valfritt',
} as const;

/** Rollnamn för en persons roll inom en upplaga. */
export const PERSON_EDITION_ROLE = {
  visitor:     'Besökare',
  organiser:   'Arrangör',
  staff:       'Funktionär',
  coordinator: 'Koordinator',
  responsible: 'Ansvarig',
} as const;

/** CSS-klasser för upplaga-rollpillar. */
export const PERSON_EDITION_ROLE_CHIP: Record<string, string> = {
  Besökare:    'chip-green',
  Arrangör:    'chip-blue',
  Funktionär:  'chip-blue',
  Koordinator: 'chip-red',
  Ansvarig:    'chip-grey',
};

/** Statuspillar för person och konto. */
export const CHIP = {
  active:     'Aktiv',
  inactive:   'Inaktiv',
  locked:     'Låst',
  hasAccount: 'Konto',
  noAccount:  'Inget konto',
  admin:      'Admin',
} as const;
