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
        logger.LogError(exception,
            "An unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}",
            httpContext.Request.Path, httpContext.Request.Method);

        var (statusCode, title, details) = ProblemDetailsHelpers.GetExceptionDetails(exception);

        httpContext.Response.StatusCode = statusCode;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Title = title, Detail = details }
        });

        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Did something!")]
    private partial void Log_DidSomething();
}
