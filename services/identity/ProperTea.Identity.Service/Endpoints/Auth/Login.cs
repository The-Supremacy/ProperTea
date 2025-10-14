using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Service.Models;
using ProperTea.Identity.Service.Services;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class Login
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
                LoginRequest request,
                UserManager<ProperTeaUser> userManager,
                SignInManager<ProperTeaUser> signInManager,
                ITokenService tokenService) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

                if (!result.Succeeded)
                {
                    return Results.Unauthorized();
                }
                
                user.LastLoginAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);

                var token = tokenService.CreateToken(user);
                var response = new AuthResponse(user.Id, user.Email!, token);

                return Results.Ok(response);
            })
            .WithName("LoginUser")
            .WithSummary("Logs in a user")
            .WithDescription("Authenticates a user and returns an access token.")
            .Produces<AuthResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}