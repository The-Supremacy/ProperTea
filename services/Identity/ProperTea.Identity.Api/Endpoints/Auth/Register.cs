using Microsoft.AspNetCore.Identity;
using ProperTea.Identity.Api.DTOs;
using ProperTea.Identity.Kernel.IntegrationEvents;
using ProperTea.Identity.Kernel.Models;
using ProperTea.Identity.Kernel.Services;
using TheSupremacy.ProperIntegrationEvents;
using TheSupremacy.ProperDomain;

namespace ProperTea.Identity.Api.Endpoints.Auth;

public static class Register
{
    public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (
                RegisterRequest request,
                UserManager<ProperTeaUser> userManager,
                ITokenService tokenService,
                IIntegrationEventPublisher eventPublisher,
                IUnitOfWork unitOfWork) =>
            {
                var user = new ProperTeaUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                    return Results.BadRequest(result.Errors);

                await eventPublisher.PublishAsync("identity-events", new UserCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    user.Id,
                    user.CreatedAt));
                await unitOfWork.SaveChangesAsync();

                var token = tokenService.CreateToken(user);
                var response = new AuthResponse(user.Id, user.Email, token);

                return Results.Created($"/api/users/{user.Id}", response);
            })
            .WithName("RegisterUser")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account and returns an access token.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .Produces<IdentityError[]>(StatusCodes.Status400BadRequest);

        return app;
    }
}