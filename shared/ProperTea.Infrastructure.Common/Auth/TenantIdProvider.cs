using Microsoft.AspNetCore.Http;

namespace ProperTea.Infrastructure.Common.Auth;

public interface IOrganizationIdProvider
{
    public string? GetOrganizationId();
}

public class TenantIdProvider(IHttpContextAccessor httpContextAccessor) : IOrganizationIdProvider
{
    private const string OrganizationIdHeader = "X-Organization-Id";

    public string? GetOrganizationId()
    {
        var organizationId = httpContextAccessor.HttpContext?.Request.Headers[OrganizationIdHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(organizationId) ? null : organizationId;
    }
}
