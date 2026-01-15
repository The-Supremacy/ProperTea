using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record GetProfileQuery(string ZitadelUserId);

public class GetProfileHandler : IWolverineHandler
{
    public async Task<UserProfileResponse?> Handle(
        GetProfileQuery query,
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
