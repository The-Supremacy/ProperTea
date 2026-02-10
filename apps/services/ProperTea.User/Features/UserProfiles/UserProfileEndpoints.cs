using System.Security.Claims;
using Marten;
using Wolverine;
using Wolverine.Http;
using ProperTea.User.Features.UserProfiles.Lifecycle;
using ProperTea.User.Features.UserProfiles.Infrastructure;

namespace ProperTea.User.Features.UserProfiles;

public record UserProfileResponse(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public record UserDetailsResponse(
    Guid? InternalId,
    string ExternalId,
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

    [WolverineGet("/users/external/{externalUserId}")]
    public static async Task<IResult> GetUserDetails(
        string externalUserId,
        IExternalUserClient externalUserClient,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Get details from Zitadel
        var userDetails = await externalUserClient.GetUserDetailsAsync(externalUserId, ct);

        if (userDetails is null)
        {
            return Results.NotFound();
        }

        // Try to get internal profile ID if it exists
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ExternalUserId == externalUserId, ct);

        return Results.Ok(new UserDetailsResponse(
            profile?.Id,
            userDetails.Id,
            userDetails.Email,
            userDetails.FirstName,
            userDetails.LastName,
            userDetails.FullName
        ));
    }
}
