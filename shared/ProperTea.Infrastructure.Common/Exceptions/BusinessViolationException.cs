namespace ProperTea.Infrastructure.Common.Exceptions;

public class BusinessViolationException : DomainException
{
    public string? FieldName { get; }

    public BusinessViolationException(string errorCode, string message, Dictionary<string, object>? parameters = null)
        : base(errorCode, message, parameters)
    {
    }

    public BusinessViolationException(string errorCode, string fieldName, string message, Dictionary<string, object>? parameters = null)
        : base(errorCode, message, parameters)
    {
        FieldName = fieldName;
        if (parameters == null)
        {
            Parameters = new Dictionary<string, object> { ["fieldName"] = fieldName };
        }
        else if (!parameters.ContainsKey("fieldName"))
        {
            parameters["fieldName"] = fieldName;
        }
    }
}
