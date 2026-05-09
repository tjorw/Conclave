export const VISITOR_REGISTRATION_STATUS_LABEL: Record<string, string> = {
  PendingPayment: 'Väntar på betalning',
  Confirmed:      'Bekräftad',
  Cancelled:      'Avbokad',
};

export const VISITOR_REGISTRATION_STATUS_CHIP: Record<string, string> = {
  PendingPayment: 'chip-orange',
  Confirmed:      'chip-green',
  Cancelled:      'chip-grey',
};

export const SESSION_REGISTRATION_STATUS_LABEL: Record<string, string> = {
  Confirmed: 'Bekräftad',
  Cancelled: 'Avbokad',
};

export const TEAM_REGISTRATION_STATUS_LABEL: Record<string, string> = {
  Pending:   'Väntar',
  Confirmed: 'Bekräftad',
  Cancelled: 'Avbokad',
};

export const TEAM_REGISTRATION_STATUS_CHIP: Record<string, string> = {
  Pending:   'chip-orange',
  Confirmed: 'chip-green',
  Cancelled: 'chip-grey',
};

export const TICKET_PAYMENT_STATUS_LABEL: Record<string, string> = {
  PendingPayment: 'Inväntar betalning',
  Confirmed:      'Betald',
  Cancelled:      'Avbokad',
  Reserved:       'Reserverad',
  Paid:           'Betald',
  Collected:      'Uthämtad',
  Revoked:        'Makulerad',
};
