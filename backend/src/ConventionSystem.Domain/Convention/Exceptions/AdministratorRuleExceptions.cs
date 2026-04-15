using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.Exceptions;

public sealed class PersonIsAlreadyAdministratorException()
    : DomainRuleViolationException(
        "Personen är redan administratör för denna konvention.",
        "person_already_administrator");

public sealed class PersonIsNotAdministratorException()
    : DomainRuleViolationException(
        "Personen är inte administratör för denna konvention.",
        "person_not_administrator");

public sealed class CannotRemoveSelfAsAdministratorException()
    : DomainRuleViolationException(
        "Du kan inte ta bort dig själv som administratör.",
        "cannot_remove_self_as_administrator");
