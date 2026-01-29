using ProperTea.Infrastructure.Common.Exceptions;

namespace ProperTea.Infrastructure.Common.ErrorHandling;

public static class ProblemDetailsHelpers
{
    public static (int StatusCode, string Title, string Details, string? ErrorCode, Dictionary<string, object>? Parameters) GetExceptionDetails(Exception exception)
    {
        var errorCode = GetErrorCode(exception);
        var parameters = GetParameters(exception);

        return (
            StatusCodeMapping.GetStatusCode(exception),
            StatusCodeMapping.GetTitle(exception),
            StatusCodeMapping.GetDetail(exception),
            errorCode,
            parameters
        );
    }

    private static string? GetErrorCode(Exception exception)
    {
        return exception switch
        {
            DomainException domainEx => domainEx.ErrorCode,
            FluentValidation.ValidationException validationEx =>
                validationEx.Errors.FirstOrDefault()?.ErrorCode ?? "VALIDATION.ERROR",
            _ => null
        };
    }

    private static Dictionary<string, object>? GetParameters(Exception exception)
    {
        return exception switch
        {
            DomainException domainEx => domainEx.Parameters,
            FluentValidation.ValidationException validationEx => new Dictionary<string, object>
            {
                ["errors"] = validationEx.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    errorCode = e.ErrorCode,
                    message = e.ErrorMessage,
                    attemptedValue = e.AttemptedValue
                }).ToList()
            },
            _ => null
        };
    }
}
