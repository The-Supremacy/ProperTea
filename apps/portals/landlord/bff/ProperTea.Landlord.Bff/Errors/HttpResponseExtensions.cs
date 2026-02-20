namespace ProperTea.Landlord.Bff.Errors;

public static class HttpResponseExtensions
{
    /// <summary>
    /// Like <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> but throws
    /// <see cref="DownstreamApiException"/> instead so the BFF can proxy the downstream
    /// ProblemDetails body verbatim to the client.
    /// </summary>
    public static async Task EnsureSuccessOrProxyAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString();
        throw new DownstreamApiException((int)response.StatusCode, body, contentType);
    }
}
