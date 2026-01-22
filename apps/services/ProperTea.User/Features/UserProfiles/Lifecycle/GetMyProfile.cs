using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record GetProfileQuery(string ExternalUserId);

public class GetProfileHandler : IWolverineHandler
{
    public async Task<UserProfileResponse?> Handle(
        GetProfileQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ExternalUserId == query.ExternalUserId, ct);

        if (profile is null)
        {
            return null;
        }

        return new UserProfileResponse(
            profile.Id,
            profile.ExternalUserId,
            profile.CreatedAt,
            profile.LastSeenAt
        );
    }
}
