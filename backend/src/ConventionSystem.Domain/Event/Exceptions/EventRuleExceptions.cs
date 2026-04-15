using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Event.Exceptions;

public sealed class EventIsCancelledException()
    : DomainRuleViolationException(
        "Evenemanget är inställt.",
        "event_is_cancelled");

public sealed class EventIsCancelledAndReadOnlyException()
    : DomainRuleViolationException(
        "Evenemanget är inställt och kan inte redigeras.",
        "event_is_cancelled_and_read_only");

public sealed class EventAlreadyUnderReviewException()
    : DomainRuleViolationException(
        "Evenemanget är redan under granskning.",
        "event_already_under_review");

public sealed class EventAlreadyPublishedException()
    : DomainRuleViolationException(
        "Evenemanget är redan publicerat.",
        "event_already_published");

public sealed class EventAlreadyDraftException()
    : DomainRuleViolationException(
        "Evenemanget är redan i utkastläge.",
        "event_already_draft");

public sealed class EventNotUnderReviewException()
    : DomainRuleViolationException(
        "Evenemanget är inte under granskning.",
        "event_not_under_review");

public sealed class EventAlreadyCancelledException()
    : DomainRuleViolationException(
        "Evenemanget är redan inställt.",
        "event_already_cancelled");

public sealed class EventTitleRequiredException()
    : DomainRuleViolationException(
        "Evenemanget måste ha en titel.",
        "event_title_required");

public sealed class EventDescriptionRequiredException()
    : DomainRuleViolationException(
        "Evenemanget måste ha en beskrivning.",
        "event_description_required");

public sealed class SessionRequestNotFoundException()
    : DomainRuleViolationException(
        "Sessionönskemålet hittades inte.",
        "session_request_not_found");

public sealed class SessionNotFoundException()
    : DomainRuleViolationException(
        "Sessionen hittades inte.",
        "session_not_found");

public sealed class CoOrganiserAlreadyAddedException()
    : DomainRuleViolationException(
        "Personen är redan medarrangör för detta evenemang.",
        "coorganiser_already_added");

public sealed class SessionInactiveCannotEditException()
    : DomainRuleViolationException(
        "Kan inte redigera en inaktiv session.",
        "session_inactive_cannot_edit");

public sealed class SessionAlreadyInactiveException()
    : DomainRuleViolationException(
        "Sessionen är redan inaktiv.",
        "session_already_inactive");
