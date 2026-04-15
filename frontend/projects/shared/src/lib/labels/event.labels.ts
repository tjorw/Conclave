export const EVENT_STATUS_LABEL: Record<string, string> = {
  Draft:       'Utkast',
  UnderReview: 'Under granskning',
  Published:   'Publicerat',
  Cancelled:   'Inställt',
};

export const EVENT_STATUS_CHIP: Record<string, string> = {
  Draft:       'chip-grey',
  UnderReview: 'chip-orange',
  Published:   'chip-green',
  Cancelled:   'chip-grey',
};

export const REGISTRATION_KIND_LABEL: Record<string, string> = {
  DropIn:          'Drop-in',
  PreRegistration: 'Föranmälan',
  Combined:        'Kombinerat',
};

export const START_TYPE_LABEL: Record<string, string> = {
  FixedTime:  'Fast tid',
  Rolling:    'Löpande',
  Tournament: 'Turneringsformat',
};

export const SESSION_STATUS_LABEL: Record<string, string> = {
  Active:   'Aktiv',
  Inactive: 'Inaktiv',
};

export const EVENT_COMMENT_STATUS_LABEL: Record<string, string> = {
  New:          'Ny',
  InProgress:   'Under behandling',
  Responded:    'Besvarad',
  Acknowledged: 'Kvitterad',
};
