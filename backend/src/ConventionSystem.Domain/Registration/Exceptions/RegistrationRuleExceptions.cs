using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Registration.Exceptions;

public sealed class TicketNotReservedForPaymentException()
    : DomainRuleViolationException(
        "Biljetten kan bara betalas i reserverat läge.",
        "ticket_not_reserved_for_payment");

public sealed class TicketNotPaidForCollectionException()
    : DomainRuleViolationException(
        "Biljetten måste vara betald för att kunna hämtas ut.",
        "ticket_not_paid_for_collection");

public sealed class TicketAlreadyRevokedException()
    : DomainRuleViolationException(
        "Biljetten är redan makulerad.",
        "ticket_already_revoked");

public sealed class AvailabilityNotFoundException()
    : DomainRuleViolationException(
        "Tillgängligheten hittades inte.",
        "availability_not_found");

public sealed class DuplicateStationPreferenceException()
    : DomainRuleViolationException(
        "Stationsönskemål för denna station finns redan.",
        "duplicate_station_preference");

public sealed class StationPreferenceNotFoundException()
    : DomainRuleViolationException(
        "Stationsönskemålet hittades inte.",
        "station_preference_not_found");

public sealed class StaffApplicationCannotBeAcceptedInCurrentStateException()
    : DomainRuleViolationException(
        "Bara mottagna eller granskade ansökningar kan accepteras.",
        "staff_application_cannot_be_accepted_in_current_state");

public sealed class StaffApplicationCannotBeRejectedInCurrentStateException()
    : DomainRuleViolationException(
        "Bara mottagna eller granskade ansökningar kan avslås.",
        "staff_application_cannot_be_rejected_in_current_state");

public sealed class SessionRegistrationAlreadyCancelledException()
    : DomainRuleViolationException(
        "Sessionsregistreringen är redan avbokad.",
        "session_registration_already_cancelled");

public sealed class VisitorRegistrationPaymentStateInvalidException()
    : DomainRuleViolationException(
        "Betalning kan bara bekräftas när registreringen väntar på betalning.",
        "visitor_registration_payment_state_invalid");

public sealed class VisitorRegistrationAlreadyCancelledException()
    : DomainRuleViolationException(
        "Registreringen är redan avbokad.",
        "visitor_registration_already_cancelled");

public sealed class TicketPerkNotFoundException()
    : DomainRuleViolationException(
        "Förmånen hittades inte.",
        "ticket_perk_not_found");

public sealed class TicketTypeNotFoundException()
    : DomainRuleViolationException(
        "Biljetttypen hittades inte.",
        "ticket_type_not_found");

public sealed class TicketTypeHasIssuedTicketsException()
    : DomainRuleViolationException(
        "Biljetttypen kan inte tas bort eftersom biljetter av typen redan utfärdats.",
        "ticket_type_has_issued_tickets");

public sealed class TicketValidDaysOutsideEditionPeriodException()
    : DomainRuleViolationException(
        "Alla giltiga dagar måste ligga inom upplagan.",
        "ticket_valid_days_outside_edition_period");

public sealed class TicketNotReservedForCancellationException()
    : DomainRuleViolationException(
        "Biljetten kan bara avbokas i reserverat läge.",
        "ticket_not_reserved_for_cancellation");

public sealed class TicketAlreadyPaidException()
    : DomainRuleViolationException(
        "Biljetten är redan betald.",
        "ticket_already_paid");
