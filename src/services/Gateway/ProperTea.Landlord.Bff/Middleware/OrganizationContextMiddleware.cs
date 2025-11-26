using System.Text.Json;

namespace ProperTea.Landlord.Bff.Middleware;

public class OrganizationContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var orgIdFromRoute = context.Request.RouteValues["orgId"]?.ToString();
        if (string.IsNullOrEmpty(orgIdFromRoute))
        {
            await next(context);
            return;
        }

        var organizationClaim = context.User.Claims.FirstOrDefault(c => c.Type == "organization");

        if (organizationClaim is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("User is not a member of any organization.");
            return;
        }

        try
        {
            var userOrgs = JsonSerializer.Deserialize<Dictionary<string, UserOrganizationDetails>>(organizationClaim.Value);
            var orgEntry = userOrgs?.FirstOrDefault(kvp => string.Equals(kvp.Key, orgIdFromRoute, StringComparison.OrdinalIgnoreCase));

            if (orgEntry?.Key is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync($"User is not authorized for organization '{orgIdFromRoute}'.");
                return;
            }
            
            context.Request.Headers.Append("X-Organization-Id", orgEntry.Value.Value.Id);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Invalid organization claim format.");
            return;
        }

        await next(context);
    }

    private sealed record UserOrganizationDetails(string Id);
}
