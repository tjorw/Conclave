export interface AppLabels {
  // Navigation
  navProgram: string;
  navMyPages: string;
  navLogout: string;
  navLogin: string;
  navOpenMenu: string;
  navCloseMenu: string;
  navPoweredBy: string;

  // Program list
  programTitle: string;
  programAllDays: string;
  programResultCount: (n: number) => string;
  programSessionsAbbr: string;
  programNoResults: string;

  // Login
  loginTitle: string;
  loginEmail: string;
  loginPassword: string;
  loginButton: string;
  loginForgotPassword: string;
  loginNoAccount: string;
  loginCreateAccount: string;

  // Register
  registerTitle: string;
  registerName: string;
  registerEmail: string;
  registerPassword: string;
  registerPasswordHint: string;
  registerButton: string;
  registerSuccessText: string;
  registerGoToLogin: string;
  registerAlreadyHaveAccount: string;
  registerLoginLink: string;

  // My pages hub
  hubGreeting: (name: string) => string;
  hubTitle: string;
  hubTicketsTitle: string;
  hubTicketsEmpty: string;
  hubTicketsCta: string;
  hubEventsTitle: string;
  hubEventsEmpty: string;
  hubEventsCta: string;
  hubTicketCount: (n: number) => string;
  hubLatestConfirmed: string;
  hubLatestReserved: string;
  hubLatestPricePrefix: string;
  hubTotalPricePrefix: string;
  hubPriceNotSpecified: string;
  hubEventCount: (n: number) => string;
  hubPendingCommentCount: (n: number) => string;
  hubProgramTitle: string;
  hubProgramEmpty: string;
  hubProgramCta: string;
  hubProgramNext: string;
  hubProgramSessionCount: (n: number) => string;
  hubStaffTitle: string;
  hubStaffEmpty: string;
  hubStaffCta: string;
  hubStaffSubmitted: string;
  hubStaffConfirmed: string;
  hubStaffAssigned: string;
  hubStaffPending: string;
  hubProfileTitle: string;
  hubProfileSummary: string;
  hubProfileCta: string;
  hubBack: string;

  // Profile
  profileTitle: string;
  profileWelcomeBanner: string;
  profileContactSection: string;
  profileSavedMessage: string;
  profileName: string;
  profileEmail: string;
  profilePhone: string;
  profileSaveButton: string;
  profilePasswordSection: string;
  profilePasswordSaved: string;
  profileCurrentPassword: string;
  profileNewPassword: string;
  profilePasswordHint: string;
  profileConfirmPassword: string;
  profileChangePasswordButton: string;

  // My events
  myEventsTitle: string;
  myEventsNewButton: string;
  myEventsEmpty: string;
  myEventsCreateFirst: string;
  myEventsUnnamed: string;
  myEventsSessions: (n: number) => string;
  myEventsUnknownCategory: string;
  myEventsStatusDraft: string;
  myEventsStatusUnderReview: string;
  myEventsStatusPublished: string;
  myEventsStatusCancelled: string;

  // My program
  myProgramTitle: string;
  myProgramTimeline: string;
  myProgramShowWatched: string;
  myProgramShowPast: string;
  myProgramNoItems: string;
  myProgramAllPast: string;
  myProgramShowPastButton: string;
  myProgramBooked: string;
  myProgramNoneBooked: string;
  myProgramVenuePrefix: string;
  myProgramCancelling: string;
  myProgramCancel: string;
  myProgramWatched: string;
  myProgramNoneWatched: string;
  myProgramBrowse: string;
  myProgramRemoving: string;
  myProgramRemove: string;
  myProgramUnnamed: string;
  myProgramPastChip: string;
  myProgramUnnamedSession: string;
  myProgramVenueNotSet: string;
  myProgramScheduleBooked: string;
  myProgramScheduleWatching: string;
  myProgramScheduleOrganiser: string;
  myProgramScheduleShift: string;
  myProgramShiftRoleResponsible: string;
  myProgramShiftRoleAssigned: string;
  myProgramCancelError: string;
  myProgramRemoveWatchError: string;
  myProgramUnknownTime: string;
  myProgramUnknownDay: string;

  // Forgot password
  forgotTitle: string;
  forgotDescription: string;
  forgotEmail: string;
  forgotButton: string;
  forgotSuccessText: string;
  forgotBackToLogin: string;

  // Reset password
  resetInvalidTitle: string;
  resetInvalidText: string;
  resetForgotLink: string;
  resetSuccessTitle: string;
  resetSuccessText: string;
  resetFormTitle: string;
  resetNewPassword: string;
  resetPasswordHint: string;
  resetConfirmPassword: string;
  resetButton: string;
  resetPasswordMismatchError: string;
  resetInvalidLinkError: string;

  // My staff
  myStaffTitle: string;
  myStaffApplicationStatus: string;
  myStaffStatusField: string;
  myStaffShiftsTitle: string;
  myStaffNoShifts: string;
  myStaffApplyTitle: string;
  myStaffRegistrationClosed: string;
  myStaffMotivationLabel: string;
  myStaffMotivationPlaceholder: string;
  myStaffAreasTitle: string;
  myStaffNoAreas: string;
  myStaffAvailabilityTitle: string;
  myStaffNoDates: string;
  myStaffSubmitting: string;
  myStaffSubmitButton: string;
  myStaffUnknownTime: string;
  myStaffShiftRoleResponsible: string;
  myStaffShiftRoleAssigned: string;

  // My ticket
  myTicketTitle: string;
  myTicketRegistrationsTitle: string;
  myTicketTicketLabel: (n: number) => string;
  myTicketTypeField: string;
  myTicketReferenceField: string;
  myTicketValidDaysField: string;
  myTicketCancelling: string;
  myTicketCancelButton: string;
  myTicketPromoLabel: string;
  myTicketPromoPlaceholder: string;
  myTicketRedeeming: string;
  myTicketRedeemButton: string;
  myTicketPaymentInfo: string;
  myTicketPaidCancelInfo: string;
  myTicketBookingClosedTitle: string;
  myTicketBookingClosedText: string;
  myTicketSelectTitle: string;
  myTicketNoneAvailable: string;
  myTicketVisitorLabel: string;
  myTicketLoadingTickets: string;
  myTicketTermsLabel: string;
  myTicketPaymentNote: string;
  myTicketBookButton: string;
  myTicketStatusPendingPayment: string;
  myTicketStatusConfirmed: string;
  myTicketStatusCancelled: string;
  myTicketStatusPaid: string;
  myTicketStatusCollected: string;
  myTicketCategoryOrganiser: string;
  myTicketCategoryStaff: string;
  myTicketCategoryVisitor: string;
  myTicketDefaultTypeLabel: string;
  myTicketPriceMissing: string;

  // Event detail
  eventDetailBreadcrumb: string;
  eventDetailSessionsTitle: string;
  eventDetailNoSessions: string;
  eventDetailSeatsLabel: string;
  eventDetailFixedTime: string;
  eventDetailLoginToRegister: string;
  eventDetailGetTicket: string;
  eventDetailCancelSession: string;
  eventDetailRegisterSession: string;
  eventDetailViewInProgram: string;
  eventDetailAboutTitle: string;
  eventDetailUnknownCategory: string;
  eventDetailWatchAriaLabel: string;
  eventDetailUnwatchAriaLabel: string;
  eventDetailLoginForTeam: string;
  eventDetailRegisterTeam: string;
  eventDetailCapacityHigh: string;
  eventDetailCapacityAlmost: string;
  eventDetailCapacityGood: string;
  eventDetailNotFound: string;
  eventDetailTicketRequired: string;
  eventDetailRegisterError: string;
  eventDetailCancelError: string;
  eventDetailWatchError: string;
  eventDetailUnwatchError: string;

  // Public page
  publicPageNotFound: string;
  publicPageBackHome: string;

  // Register errors
  registerFailedError: string;

  // Confirm email
  confirmLoading: string;
  confirmSuccessTitle: string;
  confirmSuccessText: string;
  confirmLoginButton: string;
  confirmFailTitle: string;
  confirmFailText: string;
  confirmResentText: string;
  confirmResendButton: string;
  confirmBackToLogin: string;
}
