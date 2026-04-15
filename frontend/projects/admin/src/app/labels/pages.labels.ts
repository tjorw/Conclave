/** Sidspecifika texter: rubriker, felsidor, tomma tillstånd. */
export const ERROR_PAGE = {
  '401': {
    code:        '401',
    title:       'Inte inloggad',
    description: 'Du måste logga in för att komma åt den här sidan.',
    action:      'Gå till inloggning',
  },
  '403': {
    code:        '403',
    title:       'Ingen behörighet',
    description: 'Du har inte behörighet att se den här sidan. Adminrättigheter krävs.',
    action:      'Till inloggning',
  },
  '404': {
    code:        '404',
    title:       'Sidan hittades inte',
    description: 'Sidan du letar efter finns inte eller har flyttats.',
    action:      'Tillbaka till start',
  },
} as const;
