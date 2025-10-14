using Microsoft.Extensions.Caching.Distributed;
using ProperTea.Landlord.Bff.Middleware;

namespace ProperTea.Landlord.Bff.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/logout", async (HttpContext context, IDistributedCache cache) =>
            {
                if (!context.Request.Cookies.TryGetValue(SessionManagementMiddleware.SessionCookieName,
                        out var sessionId)
                    || string.IsNullOrEmpty(sessionId))
                    return Results.Ok();
                
                await cache.RemoveAsync(sessionId);
                context.Response.Cookies.Delete(SessionManagementMiddleware.SessionCookieName);

                return Results.Ok();
            })
            .WithName("Logout")
            .WithSummary("Loggs out the current user")
            .WithDescription("Logs out the current user by clearing the authentication cookie.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}