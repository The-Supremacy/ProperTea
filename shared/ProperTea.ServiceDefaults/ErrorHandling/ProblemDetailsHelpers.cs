using Microsoft.AspNetCore.Http;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class ProblemDetailsHelpers
{
    public static (int StatusCode, string Title, string Details) GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            BusinessViolationException ex => (StatusCodes.Status422UnprocessableEntity, "Validation Error",
                ex.FieldName != null ? $"{ex.FieldName}: {ex.Message}" : ex.Message),
            System.ComponentModel.DataAnnotations.ValidationException ex => (StatusCodes.Status422UnprocessableEntity, "Validation Error",
                ex.Message),
            FluentValidation.ValidationException ex => (StatusCodes.Status422UnprocessableEntity, "Validation Error",
                ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            NotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource."),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout", "The request has timed out."),
            HttpRequestException httpEx => ((int?)httpEx.StatusCode ?? StatusCodes.Status502BadGateway, "Bad Gateway",
                httpEx.Message),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "")
        };
    }
}
