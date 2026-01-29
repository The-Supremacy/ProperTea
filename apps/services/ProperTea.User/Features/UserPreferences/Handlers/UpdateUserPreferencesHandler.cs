using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserPreferences.Handlers;

public class UpdateUserPreferencesHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateUserPreferencesCommand command,
        IDocumentSession session,
        ILogger<UpdateUserPreferencesHandler> logger,
        CancellationToken ct)
    {
        var existing = await session.Query<UserPreferencesAggregate>()
            .Where(p => p.ExternalUserId == command.ExternalUserId)
            .FirstOrDefaultAsync(ct);

        if (existing == null)
        {
            var newId = Guid.NewGuid();
            var preferencesUpdated = UserPreferencesAggregate.UpdatePreferences(
                command.ExternalUserId,
                command.Theme,
                command.Language
            );

            _ = session.Events.StartStream<UserPreferencesAggregate>(newId, preferencesUpdated);

            logger.LogInformation(
                "Created user preferences for external user {ExternalUserId}: Theme={Theme}, Language={Language}",
                command.ExternalUserId,
                command.Theme,
                command.Language
            );
        }
        else
        {
            var preferencesUpdated = UserPreferencesAggregate.UpdatePreferences(
                command.ExternalUserId,
                command.Theme,
                command.Language
            );

            _ = session.Events.Append(existing.Id, preferencesUpdated);

            logger.LogInformation(
                "Updated user preferences for external user {ExternalUserId}: Theme={Theme}, Language={Language}",
                command.ExternalUserId,
                command.Theme,
                command.Language
            );
        }

        await session.SaveChangesAsync(ct);
    }
}
