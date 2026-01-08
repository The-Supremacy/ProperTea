using Microsoft.AspNetCore.Http;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class CorrelationIdProvider
{
    public static string GetOrCreate(HttpContext httpContext)
    {
        var correlationId = httpContext.Request.Headers[HttpHeaders.CorrelationId].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            httpContext.Response.Headers.Append(HttpHeaders.CorrelationId, correlationId);
        }

        return correlationId;
    }
}
