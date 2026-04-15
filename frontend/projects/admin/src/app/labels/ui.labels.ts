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
  open:        'Öppna',
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
} as const;

/** Formulärfältsetiketter (mat-label). */
export const FIELD = {
  name:             'Namn',
  email:            'E-post',
  emailAddress:     'E-postadress',
  phone:            'Telefon',
  description:      'Beskrivning',
  title:            'Titel',
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

/** Statuspillar för person och konto. */
export const CHIP = {
  active:     'Aktiv',
  inactive:   'Inaktiv',
  locked:     'Låst',
  hasAccount: 'Konto',
  noAccount:  'Inget konto',
  admin:      'Admin',
} as const;
