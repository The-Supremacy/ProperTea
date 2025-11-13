using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheSupremacy.ProperErrorHandling;

public class ProperGlobalExceptionHandler(
    IOptions<ErrorHandlingOptions> options,ILogger<ProperGlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdProvider.GetOrCreate(httpContext);

        logger.LogError(exception,
            "An unhandled exception occurred. CorrelationId: {CorrelationId}, RequestPath: {RequestPath}, Method: {Method}",
            correlationId, httpContext.Request.Path, httpContext.Request.Method);

        var problemDetails = ProblemDetailsHelpers.CreateExceptionProblemDetails(httpContext, exception, options.Value.ProblemDetailsTypeBaseUrl, options.Value.ServiceName);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers.Append(HttpHeaders.CorrelationId, correlationId);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}