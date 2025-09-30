// Middleware/InternalTokenMiddleware.cs
using ProperTea.Gateway.Services;

namespace ProperTea.Gateway.Middleware;

public class InternalTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IInternalTokenService _tokenService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<InternalTokenMiddleware> _logger;

    public InternalTokenMiddleware(
        RequestDelegate next,
        IInternalTokenService tokenService,
        IAuthorizationService authorizationService,
        ILogger<InternalTokenMiddleware> logger)
    {
        _next = next;
        _tokenService = tokenService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip health checks and JWKs
        if (context.Request.Path.StartsWithSegments("/health") || 
            context.Request.Path.StartsWithSegments("/.well-known"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("sub")?.Value;
        var organizationId = context.Items["organizationId"]?.ToString();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(organizationId))
        {
            await _next(context);
            return;
        }

        try
        {
            var permissions = await _authorizationService.GetPermissionsAsync(userId, organizationId);
            var internalToken = _tokenService.CreateToken(userId, organizationId, permissions);
            context.Request.Headers.Authorization = $"Bearer {internalToken}";
            _logger.LogDebug("Minted internal token for user {UserId} in org {OrgId}", userId, organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mint internal token for user {UserId} in org {OrgId}", userId, organizationId);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Authentication service unavailable");
            return;
        }

        await _next(context);
    }
}
