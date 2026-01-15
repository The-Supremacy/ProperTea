using Marten;

namespace ProperTea.User.Features.UserProfiles.GetMyProfile;

/// <summary>
/// Pure query - returns null if profile doesn't exist.
/// No side effects (no creation, no last seen update).
/// </summary>
public record GetMyProfileQuery(string ZitadelUserId);

public static class GetMyProfileHandler
{
    public static async Task<UserProfileResponse?> Handle(
        GetMyProfileQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ZitadelUserId == query.ZitadelUserId, ct);

        if (profile is null)
        {
            return null;
        }

        return new UserProfileResponse(
            profile.Id,
            profile.ZitadelUserId,
            profile.CreatedAt,
            profile.LastSeenAt
        );
    }
}
