using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProperTea.Infrastructure.Common.ErrorHandling;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, details, errorCode, parameters) = ProblemDetailsHelpers.GetExceptionDetails(exception);

        logger.LogInformation(
            "Handling exception {ExceptionType} -> Status {StatusCode}, ErrorCode: {ErrorCode}",
            exception.GetType().Name,
            statusCode,
            errorCode ?? "N/A");

        LogUnexpectedError(httpContext.Request.Path, httpContext.Request.Method, exception);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = details
            },
            Exception = exception
        };

        // Add errorCode and parameters to ProblemDetails extensions for frontend consumption
        if (errorCode != null)
        {
            problemDetails.ProblemDetails.Extensions["errorCode"] = errorCode;
        }

        if (parameters != null)
        {
            problemDetails.ProblemDetails.Extensions["parameters"] = parameters;
        }

        return await problemDetailsService.TryWriteAsync(problemDetails).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred. RequestPath: `{RequestPath}`, Method: `{Method}`")]
    private partial void LogUnexpectedError(
        string requestPath,
        string method,
        Exception exception);
}
