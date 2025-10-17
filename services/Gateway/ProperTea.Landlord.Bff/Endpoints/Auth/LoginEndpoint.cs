using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProperTea.Landlord.Bff.DTOs.Auth;
using ProperTea.Landlord.Bff.Middleware;
using ProperTea.Landlord.Bff.Models;
using ProperTea.Landlord.Bff.Services;

namespace ProperTea.Landlord.Bff.Endpoints.Auth;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
                LoginRequest request,
                IIdentityService identityService,
                IDistributedCache cache,
                HttpContext context) =>
        {
            var loginResponse = await identityService.LoginAsync(request);

            if (loginResponse is null)
            {
                return Results.Unauthorized();
            }

            var enrichedJwt = loginResponse.AccessToken;
            
            var sessionId = $"session:{Guid.NewGuid()}";
            var userSession = new UserSession
            {
                SessionId = sessionId,
                UserId = loginResponse.UserId,
                EnrichedJwt = enrichedJwt,
                CreatedAt = DateTime.UtcNow,
                LastRefreshedAt = DateTime.UtcNow,
                DeviceInfo = new DeviceInfo
                {
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent
                }
            };

            await cache.SetStringAsync(sessionId, JsonSerializer.Serialize(userSession), new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromDays(7)
            });

            context.Response.Cookies.Append(SessionManagementMiddleware.SessionCookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(7)
            });

            return Results.Ok(new { message = "Login successful." });
        })
        .WithName("Login")
        .WithSummary("Logs the user in and establishes a session.");

        return app;
    }
}