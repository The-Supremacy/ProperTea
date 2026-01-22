using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record UpdateLastSeenCommand(string ExternalUserId);

public class UpdateLastSeenHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateLastSeenCommand command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ExternalUserId == command.ExternalUserId, cancellationToken);

        if (profile is null)
        {
            // Profile doesn't exist - ignore gracefully
            return;
        }

        var lastSeenUpdated = profile.UpdateLastSeen();
        _ = session.Events.Append(profile.Id, lastSeenUpdated);
        await session.SaveChangesAsync(cancellationToken);
    }
}
