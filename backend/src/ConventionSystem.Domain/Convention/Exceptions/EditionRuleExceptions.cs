using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.Exceptions;

public sealed class EditionAlreadyPublishedException()
    : DomainRuleViolationException(
        "Upplagan är redan publicerad.",
        "edition_already_published");

public sealed class EditionStaffCoordinatorRequiredException()
    : DomainRuleViolationException(
        "Upplagan måste ha en bemanningskoordinator innan den kan publiceras.",
        "edition_staff_coordinator_required");

public sealed class EditionEventCoordinatorRequiredException()
    : DomainRuleViolationException(
        "Upplagan måste ha en evenemangskoordinator innan den kan publiceras.",
        "edition_event_coordinator_required");

public sealed class OrganiserRegistrationAlreadyOpenException()
    : DomainRuleViolationException(
        "Arrangörsregistrering är redan öppen.",
        "organiser_registration_already_open");

public sealed class StaffRegistrationAlreadyOpenException()
    : DomainRuleViolationException(
        "Personalregistrering är redan öppen.",
        "staff_registration_already_open");

public sealed class VisitorRegistrationAlreadyOpenException()
    : DomainRuleViolationException(
        "Besökarregistrering är redan öppen.",
        "visitor_registration_already_open");

public sealed class EditionMustBePublishedException()
    : DomainRuleViolationException(
        "Upplagan måste vara publicerad innan registrering kan öppnas.",
        "edition_must_be_published");

public sealed class OrganiserRegistrationNotOpenException()
    : DomainRuleViolationException(
        "Arrangörsregistrering är inte öppen.",
        "organiser_registration_not_open");

public sealed class StaffRegistrationNotOpenException()
    : DomainRuleViolationException(
        "Staffregistrering är inte öppen.",
        "staff_registration_not_open");

public sealed class VisitorRegistrationNotOpenException()
    : DomainRuleViolationException(
        "Besökarregistrering är inte öppen.",
        "visitor_registration_not_open");

public sealed class EditionMustBeDraftToCopyStructureException()
    : DomainRuleViolationException(
        "Kan bara kopiera struktur till en upplaga med status Utkast.",
        "edition_must_be_draft_to_copy_structure");

public sealed class StaffAreaNotFoundInEditionException()
    : DomainRuleViolationException(
        "Funktionsområdet hittades inte på denna upplaga.",
        "staff_area_not_found_in_edition");

public sealed class StationNotFoundInEditionException()
    : DomainRuleViolationException(
        "Stationen hittades inte på denna upplaga.",
        "station_not_found_in_edition");

public sealed class VenueNotFoundInEditionException()
    : DomainRuleViolationException(
        "Lokalen hittades inte på denna upplaga.",
        "venue_not_found_in_edition");

public sealed class CategoryNotFoundInEditionException()
    : DomainRuleViolationException(
        "Kategorin hittades inte på denna upplaga.",
        "category_not_found_in_edition");
