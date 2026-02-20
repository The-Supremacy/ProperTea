using Microsoft.AspNetCore.Diagnostics;

namespace ProperTea.Landlord.Bff.Errors;

/// <summary>
/// Handles <see cref="DownstreamApiException"/> by replaying the downstream status code and
/// ProblemDetails body verbatim to the Angular client. Must be registered before the shared
/// GlobalExceptionHandler so it intercepts these exceptions first.
/// </summary>
public sealed class DownstreamExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DownstreamApiException downstream)
            return false;

        httpContext.Response.StatusCode = downstream.StatusCode;
        httpContext.Response.ContentType = downstream.ContentType ?? "application/problem+json";
        await httpContext.Response.WriteAsync(downstream.Body, cancellationToken);
        return true;
    }
}
