namespace ProperTea.Infrastructure.Common.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string errorCode, string message, Dictionary<string, object>? parameters = null)
        : base(errorCode, message, parameters)
    {
    }

    public ConflictException(string errorCode, string message, Exception innerException, Dictionary<string, object>? parameters = null)
        : base(errorCode, message, innerException, parameters)
    {
    }
}
