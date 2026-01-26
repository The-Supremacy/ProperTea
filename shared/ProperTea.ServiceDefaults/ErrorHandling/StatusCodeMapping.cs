using System.Net;
using Microsoft.AspNetCore.Http;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class StatusCodeMapping
{
    public static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            BusinessViolationException => StatusCodes.Status422UnprocessableEntity,
            FluentValidation.ValidationException => StatusCodes.Status422UnprocessableEntity,
            System.ComponentModel.DataAnnotations.ValidationException => StatusCodes.Status422UnprocessableEntity,
            ConflictException => StatusCodes.Status409Conflict,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            HttpRequestException httpEx => (int?)httpEx.StatusCode ?? StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    public static Exception CreateException(HttpStatusCode statusCode, string message)
    {
        return statusCode switch
        {
            HttpStatusCode.Conflict => new ConflictException(message),
            HttpStatusCode.UnprocessableEntity or HttpStatusCode.UnprocessableContent => new BusinessViolationException(message),
            HttpStatusCode.NotFound => new NotFoundException(message),
            HttpStatusCode.BadRequest => new ArgumentException(message),
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),
            _ => new HttpRequestException(message, null, statusCode)
        };
    }

    public static string GetTitle(Exception exception)
    {
        return exception switch
        {
            BusinessViolationException => "Validation Error",
            FluentValidation.ValidationException => "Validation Error",
            System.ComponentModel.DataAnnotations.ValidationException => "Validation Error",
            ConflictException => "Conflict",
            NotFoundException => "Not Found",
            UnauthorizedAccessException => "Unauthorized",
            ArgumentException => "Bad Request",
            InvalidOperationException => "Bad Request",
            TimeoutException => "Request Timeout",
            HttpRequestException => "Bad Gateway",
            _ => "Internal Server Error"
        };
    }

    public static string GetDetail(Exception exception)
    {
        return exception switch
        {
            BusinessViolationException ex => ex.FieldName != null
                ? $"{ex.FieldName}: {ex.Message}"
                : ex.Message,

            FluentValidation.ValidationException ex => string.Join("; ",
                ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),

            UnauthorizedAccessException => "Authentication is required to access this resource.",

            TimeoutException => "The request has timed out.",

            _ when exception.Message.Length > 0 => exception.Message,

            _ => "An unexpected error occurred."
        };
    }
}
