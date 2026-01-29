using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ProperTea.Infrastructure.Common.ErrorHandling;

public static class HttpResponseExtensions
{
    public static async Task EnsureDownstreamSuccessAsync(
        this HttpResponseMessage response,
        CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        string? detail = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            detail = problem?.Detail;
        }
        catch
        {
            detail = response.ReasonPhrase;
        }

        var message = detail ?? $"Upstream error: {response.StatusCode}";

        throw StatusCodeMapping.CreateException(response.StatusCode, message);
    }
}
