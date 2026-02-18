using System.Security.Claims;
using Marten;
using Wolverine;
using Wolverine.Http;
using ProperTea.User.Features.UserProfiles.Lifecycle;
using ProperTea.User.Features.UserProfiles.Infrastructure;

namespace ProperTea.User.Features.UserProfiles;

public record UserProfileResponse(
    string UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public record UserDetailsResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string DisplayName
);

public static class UserProfileEndpoints
{
    [WolverineGet("/users/me")]
    public static async Task<IResult> GetMyProfile(
        ClaimsPrincipal user,
        IMessageBus bus)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var query = new GetProfileQuery(userId);
        var result = await bus.InvokeAsync<UserProfileResponse?>(query);

        if (result is null)
        {
            var createCommand = new CreateProfileCommand(userId);
            _ = await bus.InvokeAsync<CreateProfileResult>(createCommand);

            result = await bus.InvokeAsync<UserProfileResponse?>(query);

            if (result is null)
            {
                return Results.Problem("Failed to create user profile");
            }
        }

        await bus.PublishAsync(new UpdateLastSeenCommand(userId));

        return Results.Ok(result);
    }

    [WolverineGet("/users/{userId}")]
    public static async Task<IResult> GetUserDetails(
        string userId,
        IExternalUserClient externalUserClient,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Get details from Zitadel
        var userDetails = await externalUserClient.GetUserDetailsAsync(userId, ct);

        if (userDetails is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new UserDetailsResponse(
            userDetails.Id,
            userDetails.Email,
            userDetails.FirstName,
            userDetails.LastName,
            userDetails.FullName
        ));
    }
}
