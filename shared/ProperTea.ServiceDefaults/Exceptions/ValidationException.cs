namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when input validation fails (format, length, required fields, etc.).
/// </summary>
public class ValidationException : DomainException
{
    public string? FieldName { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }
}
