using Wolverine;
using ProperTea.User.Extensions;

namespace ProperTea.User.Features.UserProfiles.Configuration;

public static class UserProfileMessagingConfiguration
{
    public static void ConfigureUserProfileIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<UserProfileIntegrationEvents.UserProfileCreatedEvent>(
            "user.events");
    }
}
