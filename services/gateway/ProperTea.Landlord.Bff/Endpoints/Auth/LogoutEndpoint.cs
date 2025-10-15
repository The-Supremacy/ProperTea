using Microsoft.Extensions.Caching.Distributed;
using ProperTea.Landlord.Bff.Middleware;

namespace ProperTea.Landlord.Bff.Endpoints.Auth;

public static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogoutEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (HttpContext context, IDistributedCache cache) =>
            {
                if (context.Request.Cookies.TryGetValue(SessionManagementMiddleware.SessionCookieName, out var sessionId)
                    && !string.IsNullOrEmpty(sessionId))
                {
                    await cache.RemoveAsync(sessionId);
                    context.Response.Cookies.Delete(SessionManagementMiddleware.SessionCookieName);
                }

                return Results.Ok();
            })
            .WithName("Logout")
            .WithSummary("Logs out the current user by clearing the session.");

        return app;
    }
}