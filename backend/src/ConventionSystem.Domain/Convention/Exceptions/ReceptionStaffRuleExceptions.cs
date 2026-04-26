using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Convention.Exceptions;

public sealed class PersonAlreadyReceptionStaffException()
    : DomainRuleViolationException(
        "Personen har redan receptionsrollen för denna upplaga.",
        "person_already_reception_staff");

public sealed class PersonNotReceptionStaffException()
    : DomainRuleViolationException(
        "Personen har inte receptionsrollen för denna upplaga.",
        "person_not_reception_staff");
