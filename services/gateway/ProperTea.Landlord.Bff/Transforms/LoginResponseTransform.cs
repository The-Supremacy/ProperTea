using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProperTea.Landlord.Bff.Middleware;
using ProperTea.Landlord.Bff.Models;
using Yarp.ReverseProxy.Transforms;

namespace ProperTea.Landlord.Bff.Transforms;

public class LoginResponseTransform : ResponseTransform
{
    private readonly IDistributedCache _cache;

    public LoginResponseTransform(IDistributedCache cache)
    {
        _cache = cache;
    }

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context.ProxyResponse?.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var responseStream = await context.ProxyResponse.Content.ReadAsStreamAsync();
        var loginResponse = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream);

        if (!loginResponse.TryGetProperty("accessToken", out var accessTokenElement) || accessTokenElement.ValueKind != JsonValueKind.String ||
            !loginResponse.TryGetProperty("user", out var userElement) || userElement.ValueKind != JsonValueKind.Object ||
            !userElement.TryGetProperty("id", out var userIdElement) || userIdElement.ValueKind != JsonValueKind.String)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }

        var accessToken = accessTokenElement.GetString();
        var userId = userIdElement.GetString();

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userId))
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }
        
        var enrichedJwt = accessToken;

        var sessionId = $"session:{Guid.NewGuid()}";
        var userSession = new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            EnrichedJwt = enrichedJwt,
            CreatedAt = DateTime.UtcNow,
            LastRefreshedAt = DateTime.UtcNow,
            DeviceInfo = new DeviceInfo
            {
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.HttpContext.Request.Headers.UserAgent
            }
        };
        
        await _cache.SetStringAsync(sessionId, JsonSerializer.Serialize(userSession), new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromDays(7)
        });
        
        context.HttpContext.Response.Cookies.Append(SessionManagementMiddleware.SessionCookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(7)
        });
        
        context.SuppressResponseBody = true;
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Login successful." });
    }
}
