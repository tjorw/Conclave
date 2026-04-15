namespace ConventionSystem.Domain.Common;

public class DomainRuleViolationException : Exception
{
    public string ErrorCode { get; }

    public DomainRuleViolationException(string message)
        : this(message, "domain_rule_violation")
    {
    }

    protected DomainRuleViolationException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
