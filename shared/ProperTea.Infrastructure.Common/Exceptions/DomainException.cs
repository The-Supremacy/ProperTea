namespace ProperTea.Infrastructure.Common.Exceptions;

/// <summary>
/// Base class for all domain exceptions.
/// Includes error code for frontend i18n and optional parameters for message interpolation.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Machine-readable error code for frontend translation (e.g., "ORG.NAME_ALREADY_EXISTS")
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Optional parameters for message interpolation (e.g., { "organizationName": "Acme Corp" })
    /// </summary>
    public Dictionary<string, object>? Parameters { get; init; }

    protected DomainException(string errorCode, string message, Dictionary<string, object>? parameters = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Parameters = parameters;
    }

    protected DomainException(string errorCode, string message, Exception innerException, Dictionary<string, object>? parameters = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Parameters = parameters;
    }
}
