using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Service.DTOs;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class ResetPassword
{
    public static IEndpointRouteBuilder MapResetPasswordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reset-password", async (
                ResetPasswordRequest request,
                UserManager<ProperTeaUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user is null)
                    return Results.BadRequest("Invalid token or email.");

                var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

                if (!result.Succeeded)
                    return Results.BadRequest("Invalid token or email.");

                return Results.Ok("Password has been reset successfully.");
            })
            .WithName("ResetPassword")
            .WithSummary("Reset a user's password")
            .WithDescription("Resets a user's password using a valid reset token.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}