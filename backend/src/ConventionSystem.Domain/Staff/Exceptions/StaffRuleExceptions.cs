using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Staff.Exceptions;

public sealed class ShiftAlreadyFullyStaffedException()
    : DomainRuleViolationException(
        "Passet har redan nått maximal funktionering.",
        "shift_already_fully_staffed");

public sealed class PersonAlreadyAssignedToShiftException()
    : DomainRuleViolationException(
        "Personen är redan aktiv tilldelad detta pass.",
        "person_already_assigned_to_shift");

public sealed class StaffAssignmentNotFoundException()
    : DomainRuleViolationException(
        "Tilldelningen hittades inte.",
        "staff_assignment_not_found");

public sealed class AssignmentMustBeAssignedToConfirmException()
    : DomainRuleViolationException(
        "Tilldelningen måste vara i tilldelat läge för att bekräftas.",
        "assignment_must_be_assigned_to_confirm");

public sealed class AssignmentMustBeAssignedToRejectException()
    : DomainRuleViolationException(
        "Tilldelningen måste vara i tilldelat läge för att avvisas.",
        "assignment_must_be_assigned_to_reject");

public sealed class AssignmentAlreadyCancelledException()
    : DomainRuleViolationException(
        "Tilldelningen är redan avbokad.",
        "assignment_already_cancelled");

public sealed class RejectedAssignmentCannotBeCancelledException()
    : DomainRuleViolationException(
        "En avvisad tilldelning kan inte avbokas.",
        "rejected_assignment_cannot_be_cancelled");

