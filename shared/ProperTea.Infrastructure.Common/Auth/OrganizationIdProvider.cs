using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ProperTea.Infrastructure.Common.Auth;

public interface IOrganizationIdProvider
{
    public string? GetOrganizationId();
}

public class OrganizationIdProvider(IHttpContextAccessor httpContextAccessor) : IOrganizationIdProvider
{
    public const string OrganizationIdHeader = "X-Organization-Id";

    /// <summary>
    /// The Keycloak claim that carries organization membership.
    /// Its value is a JSON object keyed by organization ID:
    /// <c>{"&lt;orgId&gt;": {"name": "Acme Holdings"}}</c>
    /// </summary>
    public const string OrgIdClaim = "organization";

    public string? GetOrganizationId()
    {
        var organizationId = httpContextAccessor.HttpContext?.Request.Headers[OrganizationIdHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(organizationId) ? null : organizationId;
    }

    /// <summary>
    /// Parses the Keycloak <c>organization</c> claim from a <see cref="ClaimsPrincipal"/>.
    /// Returns the first organization found, or (null, null) when absent or malformed.
    /// For users not belonging to any Keycloak Organization (e.g. B2C tenants/renters),
    /// both values will be null — callers must handle this case.
    /// </summary>
    public static (string? OrgId, string? OrgName) ParseOrganizationClaim(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirst(OrgIdClaim)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(claimValue);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var orgId = prop.Name;
                var orgName = prop.Value.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString()
                    : null;
                return (orgId, orgName);
            }
        }
        catch (JsonException)
        {
            // Claim value is not valid JSON; treat as absent.
        }

        return (null, null);
    }
}
