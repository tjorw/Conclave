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

public sealed class SessionNotFoundException()
    : DomainRuleViolationException(
        "Sessionen hittades inte.",
        "session_not_found");

public sealed class CoOrganiserAlreadyAddedException()
    : DomainRuleViolationException(
        "Personen är redan medarrangör för detta evenemang.",
        "coorganiser_already_added");

public sealed class LeadOrganiserCannotBeCoOrganiserException()
    : DomainRuleViolationException(
        "Huvudarrangören kan inte läggas till som medarrangör.",
        "lead_organiser_cannot_be_coorganiser");

public sealed class CoOrganiserEmailRequiredException()
    : DomainRuleViolationException(
        "E-postadress för medarrangör måste anges.",
        "coorganiser_email_required");

public sealed class CoOrganiserNotFoundException()
    : DomainRuleViolationException(
        "Medarrangören hittades inte.",
        "coorganiser_not_found");

public sealed class SessionInactiveCannotEditException()
    : DomainRuleViolationException(
        "Kan inte redigera en inaktiv session.",
        "session_inactive_cannot_edit");

public sealed class SessionAlreadyInactiveException()
    : DomainRuleViolationException(
        "Sessionen är redan inaktiv.",
        "session_already_inactive");

public sealed class EventNotPublishedException()
    : DomainRuleViolationException(
        "Evenemanget måste vara publicerat för att kommentarer ska kunna hanteras i detta flöde.",
        "event_not_published");

public sealed class EventCommentNotFoundException()
    : DomainRuleViolationException(
        "Kommentaren hittades inte.",
        "event_comment_not_found");

public sealed class EventCommentDoesNotRequireHandlingException()
    : DomainRuleViolationException(
        "Kommentaren kräver inte hantering.",
        "event_comment_does_not_require_handling");

public sealed class EventCommentAlreadyRespondedException()
    : DomainRuleViolationException(
        "Kommentaren är redan besvarad.",
        "event_comment_already_responded");

public sealed class EventCommentResponseRequiredException()
    : DomainRuleViolationException(
        "Ett svar måste anges när kommentaren hanteras.",
        "event_comment_response_required");

public sealed class EventCommentTextRequiredException()
    : DomainRuleViolationException(
        "Kommentaren får inte vara tom.",
        "event_comment_text_required");

public sealed class EventCommentNotRespondedException()
    : DomainRuleViolationException(
        "Kommentaren måste vara besvarad innan den kan kvitteras.",
        "event_comment_not_responded");

public sealed class EventCommentAlreadyAcknowledgedException()
    : DomainRuleViolationException(
        "Kommentaren är redan kvitterad.",
        "event_comment_already_acknowledged");

public sealed class EventCommentAcknowledgeMustBeDoneByAuthorException()
    : DomainRuleViolationException(
        "Endast den som skapade kommentaren kan kvittera den.",
        "event_comment_acknowledge_must_be_done_by_author");

public sealed class EventCannotBeDeletedException()
    : DomainRuleViolationException(
        "Arrangemanget kan bara tas bort om det är i utkastläge eller inställt.",
        "event_cannot_be_deleted");

public sealed class CoOrganiserInvitationNotFoundException()
    : DomainRuleViolationException(
        "Inbjudan hittades inte.",
        "coorganiser_invitation_not_found");

public sealed class CoOrganiserLimitExceededException()
    : DomainRuleViolationException(
        "Det finns inte utrymme för fler medarrangörsinbjudningar.",
        "coorganiser_limit_exceeded");

public sealed class CoOrganiserInvitationEmailMismatchException()
    : DomainRuleViolationException(
        "Din e-postadress matchar inte inbjudan.",
        "coorganiser_invitation_email_mismatch");

public sealed class InvalidInvitationCodeException()
    : DomainRuleViolationException(
        "Inbjudningskoden är ogiltig.",
        "invalid_invitation_code");

public sealed class CoOrganiserAlreadyInvitedException()
    : DomainRuleViolationException(
        "Det finns redan en aktiv inbjudan för denna e-postadress.",
        "coorganiser_already_invited");

public sealed class EventFeaturedSortOrderRequiredException()
    : DomainRuleViolationException(
        "Sorteringsordning måste anges för utvalt evenemang.",
        "event_featured_sort_order_required");

public sealed class EventFeaturedSortOrderMustBeNonNegativeException()
    : DomainRuleViolationException(
        "Sorteringsordning för utvalt evenemang måste vara ett icke-negativt heltal.",
        "event_featured_sort_order_must_be_non_negative");

public sealed class EventFeaturedLimitExceededException()
    : DomainRuleViolationException(
        "Högst sex evenemang kan vara utvalda per upplaga.",
        "event_featured_limit_exceeded");

public sealed class TeamAlreadyAssignedToSessionException()
    : DomainRuleViolationException(
        "Laget är redan tilldelat denna session.",
        "team_already_assigned_to_session");

public sealed class TeamAssignmentNotFoundException()
    : DomainRuleViolationException(
        "Lagtilldelningen hittades inte.",
        "team_assignment_not_found");
