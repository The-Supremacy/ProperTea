using ProperTea.User.Features.UserProfiles.GetMyProfile;
using Wolverine;

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
        // Extract ZITADEL user ID from claims
        var zitadelUserId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(zitadelUserId))
        {
            return Results.Unauthorized();
        }

        var query = new GetMyProfileQuery(zitadelUserId);
        var response = await bus.InvokeAsync<UserProfileResponse>(query, ct);
        return Results.Ok(response);
    }
}
