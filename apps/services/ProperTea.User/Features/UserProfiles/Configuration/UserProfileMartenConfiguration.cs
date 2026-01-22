using Marten;
using Marten.Events.Projections;

namespace ProperTea.User.Features.UserProfiles.Configuration;

public static class UserProfileConfiguration
{
    public static void ConfigureUserProfileMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<UserProfileAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<UserProfileAggregate>()
            .Index(x => x.ExternalUserId, idx => idx.IsUnique = true);
    }
}
