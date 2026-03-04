using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ProperTea.Landlord.Bff.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

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

        _ = group.MapGet("/select_account", (string? returnUrl) =>
        {
            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/",
                Items = {
                    { "prompt", "select_account" }
                }
            });
        }).RequireAuthorization();

        // Silent re-authentication: re-issues the OIDC flow with prompt=none so Keycloak
        // returns a fresh token (including any new claims like `organization`) without
        // showing a login page. Use after operations that change the user's Keycloak state
        // — e.g. after onboarding creates a new org and adds the user as a member.
        _ = group.MapGet("/reauth", (string? returnUrl) =>
        {
            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/",
                Items = {
                    { "prompt", "none" }
                }
            });
        }).RequireAuthorization();

        return endpoints;
    }
}
