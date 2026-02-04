using Microsoft.AspNetCore.Http;

namespace ProperTea.Infrastructure.Common.Auth;

public interface IOrganizationIdProvider
{
    public string? GetOrganizationId();
}

public class OrganizationIdProvider(IHttpContextAccessor httpContextAccessor) : IOrganizationIdProvider
{
    public const string OrganizationIdHeader = "X-Organization-Id";
    public const string ZitadelOrgIdClaim = "urn:zitadel:iam:user:resourceowner:id";

    public string? GetOrganizationId()
    {
        var organizationId = httpContextAccessor.HttpContext?.Request.Headers[OrganizationIdHeader].FirstOrDefault();

        // Fallback to extracting from user claims if header is not present.
        if (string.IsNullOrWhiteSpace(organizationId))
            organizationId = httpContextAccessor.HttpContext?.User.FindFirst("urn:zitadel:iam:user:resourceowner:id")?.Value;

        return string.IsNullOrWhiteSpace(organizationId) ? null : organizationId;
    }
}
