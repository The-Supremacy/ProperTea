namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with existing data (uniqueness violations, etc.).
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
