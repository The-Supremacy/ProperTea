using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Service.DTOs;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class ForgotPassword
{
    public static IEndpointRouteBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/forgot-password", async (
                ForgotPasswordRequest request,
                UserManager<ProperTeaUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                    return Results.Ok(
                        "If an account with this email exists and is confirmed, a password reset token has been generated.");

                var token = await userManager.GeneratePasswordResetTokenAsync(user);

                // TODO: email the token to the user using an email service
                return Results.Ok(new { Token = token });
            })
            .WithName("ForgotPassword")
            .WithSummary("Initiate password reset")
            .WithDescription("Generates a password reset token for a user.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}