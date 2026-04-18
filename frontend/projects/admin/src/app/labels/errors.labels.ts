/** Felmeddelanden för admin-appen. */
export const ERROR = {
  // Hämtning
  fetchEdition:         'Kunde inte hämta upplagedata.',
  fetchEvent:           'Kunde inte hämta evenemanget.',
  fetchRegistrations:   'Kunde inte hämta registreringar.',
  fetchPersons:         'Kunde inte hämta personlistan.',
  fetchOrganisers:      'Kunde inte hämta arrangörer.',
  fetchStaff:           'Kunde inte hämta funktionärer.',
  fetchVisitors:        'Kunde inte hämta besökare.',
  fetchResponsibles:    'Kunde inte hämta ansvariga.',
  fetchStaffAreas:      'Kunde inte hämta bemanningsdata.',
  fetchDashboard:       'Kunde inte hämta konventionsdata.',
  fetchEvents:          'Kunde inte hämta evenemangslistan.',

  // Upplaga
  createEdition:        'Kunde inte skapa upplaga',
  publishEdition:       'Publicering misslyckades',
  unpublishEdition:     'Avpublicering misslyckades',
  setActiveEdition:     'Kunde inte sätta aktiv upplaga',
  updateEdition:        'Kunde inte uppdatera upplagan',
  openRegistration:     'Kunde inte öppna registrering',
  toggleRegistration:   'Kunde inte ändra registreringsstatus',

  // Lokaler
  createVenue:          'Kunde inte skapa lokal',
  updateVenue:          'Kunde inte uppdatera lokal',
  deleteVenue:          'Kunde inte ta bort lokal',

  // Funktionsområden
  createStaffArea:      'Kunde inte skapa funktionsområde',
  updateStaffArea:      'Kunde inte uppdatera funktionsområde',
  deleteStaffArea:      'Kunde inte ta bort funktionsområde',

  // Kategorier
  createCategory:       'Kunde inte skapa kategori',
  updateCategory:       'Kunde inte uppdatera kategori',
  deleteCategory:       'Kunde inte ta bort kategori',

  // Biljettyper
  createTicketType:     'Kunde inte skapa biljetttyp',
  updateTicketType:     'Kunde inte uppdatera biljetttyp',
  deleteTicketType:     'Kunde inte ta bort biljetttyp',

  // Evenemang
  changeCategory:       'Kunde inte byta kategori',
  approveEvent:         'Kunde inte godkänna evenemanget',
  rejectEvent:          'Kunde inte avvisa evenemanget',
  cancelEvent:          'Kunde inte ställa in evenemanget',
  deleteEvent:          'Kunde inte ta bort evenemanget',
  saveDraft:            'Kunde inte spara utkastet',
  addSessionRequest:    'Kunde inte lägga till sessionönskemål',
  removeSessionRequest: 'Kunde inte ta bort sessionönskemål',
  scheduleSession:      'Kunde inte schemalägga sessionen',
  saveSession:          'Kunde inte spara sessionen',
  deactivateSession:    'Kunde inte inaktivera sessionen',
  returnToDraft:        'Kunde inte återställa evenemanget till utkast',
  submitForReview:      'Kunde inte skicka in evenemanget för granskning',
  respondToComment:     'Kunde inte hantera kommentaren',
  createEvent:          'Kunde inte skapa evenemang',

  // Stationer
  createStation:          'Kunde inte skapa stationen',
  updateStation:          'Kunde inte uppdatera stationen',
  removeStation:          'Kunde inte ta bort stationen',

  // Bemanning
  fetchStaffApplications: 'Kunde inte hämta staffansökningar',
  acceptApplication:      'Kunde inte acceptera ansökan',
  rejectApplication:      'Kunde inte avslå ansökan',
  createShift:            'Kunde inte skapa pass',
  cancelShift:            'Kunde inte ställa in passet',
  assignPerson:           'Kunde inte tilldela person',
  confirmAssignment:      'Kunde inte bekräfta tilldelning',
  rejectAssignment:       'Kunde inte avslå tilldelning',
  unassignPerson:         'Kunde inte avboka tilldelning',
  addStaffMember:         'Kunde inte lägga till funktionär',

  // Registrering
  confirmPayment:         'Kunde inte bekräfta betalning',
  cancelRegistration:     'Kunde inte makulera registrering',

  // Person
  createPerson:           'Kunde inte skapa person',
  updatePerson:           'Kunde inte uppdatera person',
  deactivatePerson:       'Kunde inte avaktivera person',
  reactivatePerson:       'Kunde inte återaktivera person',
  sendResetLink:          'Kunde inte skicka återställningslänk',
  setLock:                'Kunde inte ändra kontostatus',
  setAdmin:               'Kunde inte ändra adminbehörighet',
} as const;
