using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TheSupremacy.ProperErrorHandling;

public static class ProblemDetailsHelpers
{
    public static string GetStatusCodeTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            408 => "Request Timeout",
            409 => "Conflict",
            410 => "Gone",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Error"
        };
    }

    public static string GetStatusCodeDetail(int statusCode)
    {
        return statusCode switch
        {
            400 => "The request was invalid or malformed.",
            401 => "Authentication is required to access this resource.",
            403 => "You do not have permission to access this resource.",
            404 => "The requested resource was not found.",
            405 => "The HTTP method is not allowed for this resource.",
            408 => "The request has timed out.",
            409 => "The request conflicts with the current state of the resource.",
            410 => "The requested resource is no longer available.",
            429 => "Too many requests have been sent in a given amount of time.",
            500 => "An unexpected error occurred on the server.",
            501 => "The server does not support the functionality required to fulfill the request.",
            502 => "The gateway received an invalid response from an upstream server.",
            503 => "The service is temporarily unavailable.",
            504 => "The gateway did not receive a timely response from an upstream server.",
            _ => "An error occurred while processing the request."
        };
    }

    public static ProblemDetails CreateExceptionProblemDetails(
        HttpContext httpContext,
        Exception exception,
        string? projectDetailsType,
        string? serviceName)
    {
        var (statusCode, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource."),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "Bad Request", argEx.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request",
                "The requested operation is not valid."),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout", "The request has timed out."),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Bad Gateway",
                "An error occurred while processing the upstream request."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        return CreateProblemDetails(httpContext, statusCode, title, detail, projectDetailsType, serviceName);
    }

    public static ProblemDetails CreateStatusCodeProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string? projectDetailsType,
        string? serviceName)
    {
        var title = GetStatusCodeTitle(statusCode);
        var detail = GetStatusCodeDetail(statusCode);

        return CreateProblemDetails(httpContext, statusCode, title, detail, projectDetailsType, serviceName);
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string? projectDetailsType,
        string? serviceName)
    {
        var correlationId = CorrelationIdProvider.GetOrCreate(httpContext);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = projectDetailsType,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };

        if (!string.IsNullOrEmpty(serviceName))
            problemDetails.Extensions["service"] = serviceName;

        httpContext.Response.Headers.Append(HttpHeaders.CorrelationId, correlationId);
        return problemDetails;
    }
}