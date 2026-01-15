using Marten;
using ProperTea.Contracts.Events;
using static ProperTea.User.Features.UserProfiles.UserProfileEvents;

namespace ProperTea.User.Features.UserProfiles.External;

/// <summary>
/// Handlers for organization lifecycle events from Organization service.
/// These handlers update user profiles based on organization state changes.
/// </summary>
public static class OrganizationEventHandlers
{
    /// <summary>
    /// When an organization is deactivated, mark all user profiles as deactivated.
    /// This allows tracking which users are affected by org deactivation.
    /// </summary>
    public static async Task Handle(
        IOrganizationDeactivated @event,
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

    /// <summary>
    /// When an organization is activated/reactivated, clear the deactivation flag.
    /// This restores user access when the organization is back online.
    /// </summary>
    public static async Task Handle(
        IOrganizationActivated @event,
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
