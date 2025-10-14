using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProperTea.Landlord.Bff.Models;
using ProperTea.Landlord.Bff.Services;

namespace ProperTea.Landlord.Bff.Middleware;

public class SessionManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionManagementMiddleware> _logger;
    public const string SessionCookieName = "properteasession";

    public SessionManagementMiddleware(RequestDelegate next, ILogger<SessionManagementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IDistributedCache cache,
        IIdentityService identityService)
    {
        if (context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) 
            && !string.IsNullOrEmpty(sessionId))
        {
            var sessionJson = await cache.GetStringAsync(sessionId);

            if (!string.IsNullOrEmpty(sessionJson))
            {
                var session = JsonSerializer.Deserialize<UserSession>(sessionJson);
                if (session != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwt = tokenHandler.ReadJwtToken(session.EnrichedJwt);
                    
                    // Reissue the token if it's expiring within the next 2 minutes
                    if (jwt.ValidTo < DateTime.UtcNow.AddMinutes(2))
                    {
                        _logger.LogInformation(
                            "JWT for user {UserId} is expiring, attempting to reissue.", session.UserId);
                        var newJwt = await identityService.ReissueTokenAsync(session.EnrichedJwt);

                        if (!string.IsNullOrEmpty(newJwt))
                        {
                            session.EnrichedJwt = newJwt;
                            session.LastRefreshedAt = DateTime.UtcNow;
                            
                            await cache.SetStringAsync(sessionId, JsonSerializer.Serialize(session));
                            _logger.LogInformation("Successfully reissued and cached new JWT for user {UserId}.", session.UserId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to reissue JWT for user {UserId}. The old token will be used.", session.UserId);
                        }
                    }
                    
                    context.Request.Headers.Authorization = $"Bearer {session.EnrichedJwt}";
                }
            }
            else
            {
                _logger.LogWarning("Session ID {SessionId} from cookie not found in Redis.", sessionId);
                // Session has expired or is invalid, clear the cookie
                context.Response.Cookies.Delete(SessionCookieName);
            }
        }

        await _next(context);
    }
}