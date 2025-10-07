using System.Security.Claims;

namespace ProperTea.Gateway.Middleware;

public class TenantValidationMiddleware
{
    private readonly ILogger<TenantValidationMiddleware> _logger;
    private readonly RequestDelegate _next;

    public TenantValidationMiddleware(RequestDelegate next, ILogger<TenantValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-tenant routes
        if (!context.Request.Path.StartsWithSegments("/api/v0/organization"))
        {
            await _next(context);
            return;
        }

        // Extract organizationId from path
        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments?.Length < 4 || pathSegments[3] == "{organizationId}")
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid organization path");
            return;
        }

        var organizationId = pathSegments[3];

        // Validate user has access to this organization
        var userOrgs = context.User.FindAll("orgs").Select(c => c.Value);
        if (!userOrgs.Contains(organizationId))
        {
            _logger.LogWarning("User {UserId} attempted to access organization {OrgId} without permission",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, organizationId);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied to organization");
            return;
        }

        // Store organization ID for downstream middleware
        context.Items["organizationId"] = organizationId;
        await _next(context);
    }
}