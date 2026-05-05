using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Content.Exceptions;

public sealed class PageMenuSortOrderMustBeNonNegativeException()
    : DomainRuleViolationException(
        "Menyordning måste vara ett icke-negativt heltal.",
        "page_menu_sort_order_must_be_non_negative");
