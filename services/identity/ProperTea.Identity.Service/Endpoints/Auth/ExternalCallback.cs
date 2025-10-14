using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Service.Models;
using ProperTea.Identity.Service.Services;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class ExternalCallback
{
    public static IEndpointRouteBuilder MapExternalCallbackEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/external/callback", async (
                SignInManager<ProperTeaUser> signInManager,
                UserManager<ProperTeaUser> userManager,
                ITokenService tokenService) =>
            {
                var info = await signInManager.GetExternalLoginInfoAsync();
                if (info is null)
                    return Results.BadRequest("Error loading external login information.");

                var signInResult = await signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider, info.ProviderKey, false, true);

                ProperTeaUser? user;
                if (signInResult.Succeeded)
                {
                    user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                }
                else
                {
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    if (string.IsNullOrEmpty(email))
                        return Results.BadRequest("Email claim not received from external provider.");

                    user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        user = new ProperTeaUser { UserName = email, Email = email, CreatedAt = DateTime.UtcNow };
                        var createResult = await userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                            return Results.BadRequest(createResult.Errors);
                    }

                    var addLoginResult = await userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                        return Results.BadRequest(addLoginResult.Errors);
                }

                if (user is null)
                    return Results.BadRequest("Failed to sign in or create user.");

                var token = tokenService.CreateToken(user);
                var response = new AuthResponse(user.Id, user.Email!, token);

                return Results.Ok(response);
            })
            .WithName("ExternalCallback")
            .WithSummary("Handle external provider callback")
            .WithDescription("Handles the callback from the external provider to complete authentication.")
            .Produces<AuthResponse>()
            .Produces<IdentityError[]>(StatusCodes.Status400BadRequest);

        return app;
    }
}