using Microsoft.AspNetCore.Http;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class ProblemDetailsHelpers
{
    public static (int StatusCode, string Title, string Details) GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            // Domain exceptions (most specific first)
            ValidationException ex => (StatusCodes.Status400BadRequest, "Validation Error",
                ex.FieldName != null ? $"{ex.FieldName}: {ex.Message}" : ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            NotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            BusinessRuleViolationException ex => (StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation", ex.Message),

            // Framework exceptions
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource."),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout", "The request has timed out."),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Bad Gateway",
                "An error occurred while processing the upstream request."),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "")
        };
    }
}
