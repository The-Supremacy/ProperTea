using Marten;

namespace ProperTea.User.Features.UserProfiles.GetMyProfile;

public record GetMyProfileQuery(string ZitadelUserId);

public static class GetMyProfileHandler
{
    public static async Task<UserProfileResponse> Handle(
        GetMyProfileQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Try to find existing profile by ZitadelUserId
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ZitadelUserId == query.ZitadelUserId, ct);

        if (profile is null)
        {
            // First time login - create profile with event
            var profileId = Guid.NewGuid();
            var created = UserProfileAggregate.Create(profileId, query.ZitadelUserId);

            _ = session.Events.StartStream<UserProfileAggregate>(profileId, created);
            await session.SaveChangesAsync(ct);

            // Load the aggregate to return
            profile = await session.Events.AggregateStreamAsync<UserProfileAggregate>(profileId, token: ct)
                ?? throw new InvalidOperationException("Failed to create user profile");
        }
        else
        {
            // Update last seen
            var lastSeenUpdated = profile.UpdateLastSeen();
            _ = session.Events.Append(profile.Id, lastSeenUpdated);
            await session.SaveChangesAsync(ct);
        }

        return new UserProfileResponse(
            profile.Id,
            profile.ZitadelUserId,
            profile.CreatedAt,
            profile.LastSeenAt
        );
    }
}
