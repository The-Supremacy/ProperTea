using ProperTea.Infrastructure.Common.Auth;

namespace ProperTea.Landlord.Bff.Auth;

public class OrganizationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var orgId = user.FindFirst(OrganizationIdProvider.ZitadelOrgIdClaim)?.Value;

            if (!string.IsNullOrWhiteSpace(orgId))
            {
                _ = request.Headers.TryAddWithoutValidation(OrganizationIdProvider.OrganizationIdHeader, orgId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
