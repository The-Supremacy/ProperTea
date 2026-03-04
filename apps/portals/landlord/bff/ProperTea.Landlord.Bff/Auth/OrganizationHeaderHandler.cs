using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using ProperTea.Infrastructure.Common.Auth;

namespace ProperTea.Landlord.Bff.Auth;

public class OrganizationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity?.IsAuthenticated != true)
            return await base.SendAsync(request, cancellationToken);

        // Prefer parsing the org claim from the raw access token — Keycloak puts
        // the `organization` claim there reliably. Reading from ClaimsPrincipal
        // (ID token / userinfo) is unreliable because the OIDC middleware flattens
        // complex JSON claim objects into dotted child-claims.
        var accessToken = await ctx.GetTokenAsync("access_token");
        string? orgId = null;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var principal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(jwt.Claims));
            orgId = OrganizationIdProvider.ParseOrganizationClaim(principal).OrgId;
        }

        if (!string.IsNullOrWhiteSpace(orgId))
        {
            _ = request.Headers.TryAddWithoutValidation(OrganizationIdProvider.OrganizationIdHeader, orgId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
