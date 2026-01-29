namespace ProperTea.Infrastructure.Common.Exceptions;

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
