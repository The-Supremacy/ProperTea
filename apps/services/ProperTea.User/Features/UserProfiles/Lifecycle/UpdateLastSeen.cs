using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

/// <summary>
/// Internal command for updating last seen timestamp.
/// Executed asynchronously via Wolverine's durable local messaging (PublishAsync).
/// </summary>
public record UpdateLastSeenCommand(string ZitadelUserId);

public class UpdateLastSeenHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateLastSeenCommand command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var profile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ZitadelUserId == command.ZitadelUserId, cancellationToken);

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
