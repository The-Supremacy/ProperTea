using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TheSupremacy.ProperErrorHandling;

public class ProperGlobalExceptionHandler(ILogger<ProperGlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                            Guid.NewGuid().ToString();

        logger.LogError(exception,
            "An unhandled exception occurred. CorrelationId: {CorrelationId}, RequestPath: {RequestPath}, Method: {Method}",
            correlationId, httpContext.Request.Path, httpContext.Request.Method);

        var problemDetails = ProblemDetailsHelpers.CreateExceptionProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.Headers.Append("X-Correlation-ID", correlationId);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}