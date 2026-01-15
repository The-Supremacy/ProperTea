using Wolverine;
using Wolverine.RabbitMQ;
using ProperTea.User.Extensions;

namespace ProperTea.User.Features.UserProfiles.Configuration;

public static class UserProfileMessagingConfiguration
{
    public static void ConfigureUserProfileMessaging(this WolverineOptions opts)
    {
        ConfigurePublications(opts);
        ConfigureSubscriptions(opts);
    }

    private static void ConfigurePublications(WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<UserProfileIntegrationEvents.UserProfileCreatedEvent>(
            "user.events",
            "user.profile-created.v1");
    }

    private static void ConfigureSubscriptions(WolverineOptions opts)
    {
        _ = opts.ListenToRabbitQueue("user.organization-events")
            .UseDurableInbox();
    }

    public static void ValidateConfiguration()
    {
        var configuredEvents = new HashSet<string>
        {
            nameof(UserProfileIntegrationEvents.UserProfileCreatedEvent)
        };

        if (configuredEvents.Count == 0)
        {
            throw new InvalidOperationException(
                "No integration events configured in UserProfileMessagingConfiguration. " +
                "Ensure ConfigurePublications() registers all user profile integration events.");
        }
    }
}
