namespace ProperTea.Landlord.Bff.Auth;

public class OrganizationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string OrganizationIdHeader = "X-Organization-Id";
    private const string ZitadelOrgIdClaim = "urn:zitadel:iam:org:id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var orgId = user.FindFirst(ZitadelOrgIdClaim)?.Value;

            if (!string.IsNullOrWhiteSpace(orgId))
            {
                _ = request.Headers.TryAddWithoutValidation(OrganizationIdHeader, orgId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
