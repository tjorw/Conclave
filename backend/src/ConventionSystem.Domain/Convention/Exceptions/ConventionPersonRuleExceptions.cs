using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.Exceptions;

public sealed class PersonDoesNotBelongToConventionException()
    : DomainRuleViolationException(
        "Personen tillhör inte denna konvention.",
        "person_does_not_belong_to_convention");

public sealed class PersonAlreadyInactiveException()
    : DomainRuleViolationException(
        "Personen är redan inaktiverad.",
        "person_already_inactive");

public sealed class PersonAlreadyActiveException()
    : DomainRuleViolationException(
        "Personen är redan aktiv.",
        "person_already_active");
