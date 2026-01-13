namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated (e.g., can't deactivate twice, can't change to same value).
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
