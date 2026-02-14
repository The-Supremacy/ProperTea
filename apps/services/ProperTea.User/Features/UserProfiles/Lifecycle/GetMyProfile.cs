using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record GetProfileQuery(string UserId);

public class GetProfileHandler : IWolverineHandler
{
    public async Task<UserProfileResponse?> Handle(
        GetProfileQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, ct);

        if (profile is null)
        {
            return null;
        }

        return new UserProfileResponse(
            profile.UserId,
            profile.CreatedAt,
            profile.LastSeenAt
        );
    }
}
