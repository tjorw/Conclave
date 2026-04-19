using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Registration.Exceptions;

public sealed class PromotionCodeAlreadyExistsException()
    : DomainRuleViolationException(
        "Kampanjkoden finns redan för upplagan.",
        "promotion_code_already_exists");

public sealed class PromotionCodeDiscountPercentageOutOfRangeException()
    : DomainRuleViolationException(
        "Procentrabatt måste vara mellan 0 och 100.",
        "promotion_code_discount_percentage_out_of_range");

public sealed class PromotionCodeInvalidValidityWindowException()
    : DomainRuleViolationException(
        "ValidFrom får inte vara senare än ValidUntil.",
        "promotion_code_invalid_validity_window");

public sealed class PromotionCodeInactiveException()
    : DomainRuleViolationException(
        "Kampanjkoden är inte aktiv.",
        "promotion_code_inactive");

public sealed class PromotionCodeMaxRedemptionsReachedException()
    : DomainRuleViolationException(
        "Kampanjkoden har nått max antal inlösningar.",
        "promotion_code_max_redemptions_reached");

public sealed class PromotionCodeNotYetValidException()
    : DomainRuleViolationException(
        "Kampanjkoden är ännu inte giltig.",
        "promotion_code_not_yet_valid");

public sealed class PromotionCodeExpiredException()
    : DomainRuleViolationException(
        "Kampanjkoden har gått ut.",
        "promotion_code_expired");

public sealed class PromotionCodeTicketTypeNotAllowedException()
    : DomainRuleViolationException(
        "Kampanjkoden gäller inte för den här biljettypen.",
        "promotion_code_ticket_type_not_allowed");

public sealed class TicketNotReservedForPromotionException()
    : DomainRuleViolationException(
        "Kampanjkod kan bara lösas in på en reserverad biljett.",
        "ticket_not_reserved_for_promotion");
