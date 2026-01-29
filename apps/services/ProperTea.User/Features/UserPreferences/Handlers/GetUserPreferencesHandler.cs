using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserPreferences.Handlers;

public class GetUserPreferencesHandler : IWolverineHandler
{
    public async Task<GetUserPreferencesResponse?> Handle(
        GetUserPreferencesQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var preferences = await session.Query<UserPreferencesAggregate>()
            .Where(p => p.ExternalUserId == query.ExternalUserId)
            .FirstOrDefaultAsync(ct);

        if (preferences == null)
        {
            return null;
        }

        return new GetUserPreferencesResponse(preferences.Theme, preferences.Language);
    }
}
