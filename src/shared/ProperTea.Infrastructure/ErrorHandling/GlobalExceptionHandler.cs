using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProperTea.Infrastructure.ErrorHandling;

public partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnexpectedError(httpContext.Request.Path, httpContext.Request.Method);

        var (statusCode, title, details) = ProblemDetailsHelpers.GetExceptionDetails(exception);

        httpContext.Response.StatusCode = statusCode;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Title = title, Detail = details }
        }).ConfigureAwait(false);

        return true;
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred. RequestPath: `{RequestPath}`, Method: `{Method}`")]
    private partial void LogUnexpectedError(
        string requestPath,
        string method);
}
