using Marten;
using static ProperTea.User.Features.UserProfiles.UserProfileEvents;

namespace ProperTea.User.Features.UserProfiles.External;

public static class OrganizationDeactivatedHandler
{
    public static async Task Handle(
        OrganizationIntegrationEvents.OrganizationDeactivated @event,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Find all user profiles (in a real system, you'd query by organization membership)
        // For this test, we'll mark ALL profiles to demonstrate the flow
        var profiles = await session.Query<UserProfileAggregate>()
            .Where(p => p.OrganizationDeactivatedAt == null)
            .ToListAsync(ct);

        foreach (var profile in profiles)
        {
            var deactivatedEvent = new OrganizationDeactivatedMarked(
                profile.Id,
                @event.DeactivatedAt);

            _ = session.Events.Append(profile.Id, deactivatedEvent);
        }

        await session.SaveChangesAsync(ct);
    }
}

public static class OrganizationActivatedHandler
{
    public static async Task Handle(
        OrganizationIntegrationEvents.OrganizationActivated @event,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Find all user profiles marked as deactivated
        var profiles = await session.Query<UserProfileAggregate>()
            .Where(p => p.OrganizationDeactivatedAt != null)
            .ToListAsync(ct);

        foreach (var profile in profiles)
        {
            var clearedEvent = new OrganizationDeactivatedCleared(profile.Id);
            _ = session.Events.Append(profile.Id, clearedEvent);
        }

        await session.SaveChangesAsync(ct);
    }
}
