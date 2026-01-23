using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ProperTea.Landlord.Bff.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        _ = group.MapGet("/user", (HttpContext context) =>
        {
            var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
            return Results.Ok(new
            {
                IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
                Claims = claims
            });
        }).RequireAuthorization();

        _ = group.MapGet("/login", (string? returnUrl) =>
        {
            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            });
        })
        .AllowAnonymous();

        _ = group.MapGet("/logout", (string? returnUrl) =>
            Results.SignOut(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization();

        _ = group.MapGet("/register", (string? returnUrl) =>
        {
            var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
            props.Parameters.Add("prompt", "create");
            return Results.Challenge(props);
        }).AllowAnonymous();

        return endpoints;
    }
}
