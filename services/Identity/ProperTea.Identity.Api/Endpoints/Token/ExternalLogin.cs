using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Identity.Kernel.Models;

namespace ProperTea.Identity.Api.Endpoints.Token;

public static class ExternalLogin
{
    public static IEndpointRouteBuilder MapExternalLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/external/{provider}", (
                string provider,
                [FromServices] SignInManager<ProperTeaUser> signInManager) =>
            {
                const string redirectUrl = "/api/auth/external/callback";
                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return Results.Challenge(properties, [provider]);
            })
            .WithName("ExternalLogin")
            .WithSummary("Initiate external provider login")
            .WithDescription("Redirects the user to the specified external authentication provider.");

        return app;
    }
}