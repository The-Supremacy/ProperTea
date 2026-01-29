using Marten;
using Marten.Events.Projections;

namespace ProperTea.User.Features.UserPreferences.Configuration;

public static class UserPreferencesMartenConfiguration
{
    public static void ConfigureUserPreferencesMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<UserPreferencesAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<UserPreferencesAggregate>()
            .Index(x => x.ExternalUserId, idx =>
            {
                idx.IsUnique = true;
            });

        opts.Events.MapEventType<UserPreferencesEvents.PreferencesUpdated>(
            "user-preferences.preferences-updated.v1"
        );
    }
}
