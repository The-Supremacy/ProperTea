using Wolverine;
using ProperTea.User.Features.UserProfiles.Lifecycle;

namespace ProperTea.User.Features.UserProfiles;

public record UserProfileResponse(
    Guid Id,
    string ZitadelUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public static class UserProfileEndpoints
{
    public static RouteGroupBuilder MapUserProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        _ = group.MapGet("/me", GetMyProfile)
            .WithName("GetMyProfile")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetMyProfile(
        HttpContext context,
        IMessageBus bus,
        CancellationToken ct)
    {
        var zitadelUserId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(zitadelUserId))
        {
            return Results.Unauthorized();
        }

        // Pure read query
        var query = new GetProfileQuery(zitadelUserId);
        var result = await bus.InvokeAsync<UserProfileResponse?>(query, ct);

        // If profile doesn't exist, create it (first login)
        if (result is null)
        {
            var createCommand = new CreateProfileCommand(zitadelUserId);
            _ = await bus.InvokeAsync<CreateProfileResult>(createCommand, ct);

            // Re-query to get the created profile
            result = await bus.InvokeAsync<UserProfileResponse?>(query, ct);

            if (result is null)
            {
                return Results.Problem("Failed to create user profile");
            }
        }

        // Update last seen asynchronously using Wolverine's durable local messaging
        // PublishAsync stores command in outbox transactionally, guarantees delivery
        await bus.PublishAsync(new UpdateLastSeenCommand(zitadelUserId));

        return Results.Ok(result);
    }
}
