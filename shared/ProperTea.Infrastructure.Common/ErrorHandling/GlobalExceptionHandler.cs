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
        var (statusCode, title, details) = ProblemDetailsHelpers.GetExceptionDetails(exception);

        logger.LogInformation(
            "Handling exception {ExceptionType} -> Status {StatusCode}",
            exception.GetType().Name,
            statusCode);

        LogUnexpectedError(httpContext.Request.Path, httpContext.Request.Method, exception);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = details
            },
            Exception = exception
        }).ConfigureAwait(false);
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
