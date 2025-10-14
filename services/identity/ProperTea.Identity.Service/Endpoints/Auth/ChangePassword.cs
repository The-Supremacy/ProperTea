using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class ChangePassword
{
    public static IEndpointRouteBuilder MapChangePasswordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/change-password", async (
                ChangePasswordRequest request,
                ClaimsPrincipal claims,
                UserManager<ProperTeaUser> userManager) =>
            {
                var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null)
                    return Results.Unauthorized();

                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                    return Results.Unauthorized();

                var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                    return Results.BadRequest(result.Errors);

                return Results.Ok("Password changed successfully.");
            })
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithSummary("Change the current user's password")
            .WithDescription("Allows an authenticated user to change their password.")
            .Produces(StatusCodes.Status200OK)
            .Produces<IdentityError[]>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}