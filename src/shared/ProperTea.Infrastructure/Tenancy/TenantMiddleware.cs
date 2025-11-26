using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProperTea.Core.Tenancy;

namespace ProperTea.Infrastructure.Tenancy;

internal sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentOrganizationProvider organizationProvider)
    {
        var claim = context.User.FindFirstValue("organization_id");

        if (Guid.TryParse(claim, out var organizationId))
        {
            organizationProvider.SetOrganization(organizationId);
        }

        await next(context);
    }
}