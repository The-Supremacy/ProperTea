using System.Security.Claims;
using Wolverine;
using Wolverine.Http;
using ProperTea.User.Features.UserProfiles.Lifecycle;

namespace ProperTea.User.Features.UserProfiles;

public record UserProfileResponse(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public static class UserProfileEndpoints
{
    [WolverineGet("/users/me")]
    public static async Task<IResult> GetMyProfile(
        ClaimsPrincipal user,
        IMessageBus bus)
    {
        var externalUserId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var query = new GetProfileQuery(externalUserId);
        var result = await bus.InvokeAsync<UserProfileResponse?>(query);

        if (result is null)
        {
            var createCommand = new CreateProfileCommand(externalUserId);
            _ = await bus.InvokeAsync<CreateProfileResult>(createCommand);

            result = await bus.InvokeAsync<UserProfileResponse?>(query);

            if (result is null)
            {
                return Results.Problem("Failed to create user profile");
            }
        }

        await bus.PublishAsync(new UpdateLastSeenCommand(externalUserId));

        return Results.Ok(result);
    }
}
