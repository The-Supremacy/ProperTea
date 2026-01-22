using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.ServiceDefaults.ErrorHandling;

public static class HttpResponseExtensions
{
    public static async Task EnsureDownstreamSuccessAsync(this HttpResponseMessage response, CancellationToken ct = default)
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

        throw response.StatusCode switch
        {
            HttpStatusCode.Conflict => new ConflictException(message),
            HttpStatusCode.UnprocessableEntity => new BusinessViolationException(message),
            HttpStatusCode.NotFound => new NotFoundException(message),
            HttpStatusCode.BadRequest => new ArgumentException(message),
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),

            _ => new HttpRequestException(message, null, response.StatusCode)
        };
    }
}
