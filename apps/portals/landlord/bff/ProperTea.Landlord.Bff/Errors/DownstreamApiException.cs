namespace ProperTea.Landlord.Bff.Errors;

/// <summary>
/// Thrown when a downstream service returns a non-success response.
/// Carries the raw status code and ProblemDetails body so the BFF can proxy it verbatim.
/// </summary>
public sealed class DownstreamApiException(int statusCode, string body, string? contentType)
    : Exception($"Downstream service returned {statusCode}")
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
    public string ContentType { get; } = contentType ?? "application/problem+json";
}
