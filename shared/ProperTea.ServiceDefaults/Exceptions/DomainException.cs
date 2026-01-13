namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Base exception for domain-level errors (business rule violations).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
