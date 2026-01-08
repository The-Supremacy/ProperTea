using Microsoft.AspNetCore.Http;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class ProblemDetailsHelpers
{
    public static (int StatusCode, string Title, string Details) GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", ""),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request", ""),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout", "The request has timed out."),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Bad Gateway",
                "An error occurred while processing the upstream request."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "")
        };
    }
}
