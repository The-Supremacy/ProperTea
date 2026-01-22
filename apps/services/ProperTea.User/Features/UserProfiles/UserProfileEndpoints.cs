using Wolverine;
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
        var externalUserId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var query = new GetProfileQuery(externalUserId);
        var result = await bus.InvokeAsync<UserProfileResponse?>(query, ct);

        if (result is null)
        {
            var createCommand = new CreateProfileCommand(externalUserId);
            _ = await bus.InvokeAsync<CreateProfileResult>(createCommand, ct);

            result = await bus.InvokeAsync<UserProfileResponse?>(query, ct);

            if (result is null)
            {
                return Results.Problem("Failed to create user profile");
            }
        }

        await bus.PublishAsync(new UpdateLastSeenCommand(externalUserId));

        return Results.Ok(result);
    }
}
