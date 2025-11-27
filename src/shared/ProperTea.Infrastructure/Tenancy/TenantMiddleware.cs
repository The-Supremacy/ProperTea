using Microsoft.AspNetCore.Http;
using ProperTea.Core.Tenancy;

namespace ProperTea.Infrastructure.Tenancy;

internal sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentOrganizationProvider organizationProvider)
    {
        const string orgIdHeaderName = "X-Organization-Id";
        if (context.Request.Headers.TryGetValue(orgIdHeaderName, out var headerValue) &&
            Guid.TryParse(headerValue, out var organizationIdFromHeader))
        {
            organizationProvider.SetOrganization(organizationIdFromHeader);
        }

        await next(context);
    }
}