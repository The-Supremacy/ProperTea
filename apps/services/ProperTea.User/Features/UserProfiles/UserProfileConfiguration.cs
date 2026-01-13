using Marten;
using Marten.Events.Projections;

namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileConfiguration
{
    public static void ConfigureUserProfileMarten(this StoreOptions opts)
    {
        // Direct aggregate snapshot using Apply methods via reflection
        _ = opts.Projections.Snapshot<UserProfileAggregate>(SnapshotLifecycle.Inline);

        // Index for ZitadelUserId lookup
        _ = opts.Schema.For<UserProfileAggregate>()
            .Index(x => x.ZitadelUserId, idx => idx.IsUnique = true);
    }
}
