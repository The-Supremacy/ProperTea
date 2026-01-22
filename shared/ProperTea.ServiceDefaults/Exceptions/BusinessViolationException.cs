namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when input validation fails (format, length, required fields, etc.).
/// </summary>
public class BusinessViolationException : DomainException
{
    public string? FieldName { get; }

    public BusinessViolationException(string message) : base(message)
    {
    }

    public BusinessViolationException(string fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }
}
