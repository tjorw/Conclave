export const STAFF_APPLICATION_STATUS_LABEL: Record<string, string> = {
  Received:    'Mottagen',
  UnderReview: 'Under granskning',
  Confirmed:   'Godkänd',
  Assigned:    'Tilldelad',
  Rejected:    'Avslagen',
};

export const STAFF_APPLICATION_STATUS_CHIP: Record<string, string> = {
  Received:    'chip chip-grey',
  UnderReview: 'chip chip-blue',
  Confirmed:   'chip chip-green',
  Assigned:    'chip chip-green',
  Rejected:    'chip chip-red',
};

export const ASSIGNMENT_STATUS_LABEL: Record<string, string> = {
  Pending:   'Väntar',
  Confirmed: 'Bekräftad',
  Rejected:  'Avslagen',
  Cancelled: 'Avbokad',
};

export const SHIFT_STATUS_LABEL: Record<string, string> = {
  Open:      'Öppet',
  Full:      'Fullt',
  Cancelled: 'Inställt',
};
export const STAFFING_STATUS_LABEL: Record<string, string> = {
  Cancelled:         'Inställt',
  Unstaffed:         'Obemannat',
  UnderMin:          'Under min',
  OverMax:           'Över max',
  Full:              'Fullbemannat',
  WithinRequirement: 'Inom behov',
};
