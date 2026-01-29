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
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

            if (!isAuthenticated)
            {
                return Results.Ok(new CurrentUserDto(
                    IsAuthenticated: false,
                    EmailAddress: string.Empty,
                    FirstName: string.Empty,
                    LastName: string.Empty,
                    OrganizationName: string.Empty
                ));
            }

            var email = context.User.FindFirst("email")?.Value ?? string.Empty;
            var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
            var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;
            var orgName = context.User.FindFirst("urn:zitadel:iam:user:resourceowner:name")?.Value ?? string.Empty;

            return Results.Ok(new CurrentUserDto(
                IsAuthenticated: true,
                EmailAddress: email,
                FirstName: firstName,
                LastName: lastName,
                OrganizationName: orgName
            ));
        }).AllowAnonymous();

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

        return endpoints;
    }
}

public record CurrentUserDto(
    bool IsAuthenticated,
    string EmailAddress,
    string FirstName,
    string LastName,
    string OrganizationName
);
