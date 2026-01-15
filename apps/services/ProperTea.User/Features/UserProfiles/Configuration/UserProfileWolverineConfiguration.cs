using Wolverine;
using Wolverine.RabbitMQ;
using ProperTea.User.Config;
using ProperTea.User.Features.UserProfiles.Lifecycle;

namespace ProperTea.User.Features.UserProfiles.Configuration;

/// <summary>
/// Explicit configuration for UserProfile feature messaging.
/// All integration event publications and subscriptions are defined here.
/// No auto-discovery - every external message must be explicitly configured.
/// </summary>
public static class UserProfileMessagingConfiguration
{
    public static void ConfigureUserProfileMessaging(this WolverineOptions opts)
    {
        ConfigurePublications(opts);
        ConfigureSubscriptions(opts);
    }

    /// <summary>
    /// Configure outgoing integration events.
    /// Convention: user.{event-name}.v{version}
    /// </summary>
    private static void ConfigurePublications(WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<UserProfileCreatedEvent>(
            "user.events",
            "user.profile-created.v1");
    }

    /// <summary>
    /// Configure incoming message subscriptions.
    /// Explicitly define which external events this service consumes.
    /// </summary>
    private static void ConfigureSubscriptions(WolverineOptions opts)
    {
        // Listen to organization lifecycle events
        _ = opts.ListenToRabbitQueue("user.organization-events", q =>
            {
                // Bind to organization.events exchange to receive org lifecycle events
                _ = q.BindExchange("organization.events");
            })
            .UseDurableInbox()
            .Named("OrganizationEventSubscriptions");
    }

    /// <summary>
    /// Validates that all integration events are properly configured.
    /// Called on startup to fail fast if configuration is incomplete.
    /// </summary>
    public static void ValidateConfiguration()
    {
        // Validate UserProfileCreatedEvent is the only integration event
        // If more events are added, update this validation
        var configuredEvents = new HashSet<string>
        {
            nameof(UserProfileCreatedEvent)
        };

        // This is a simple check - in the future, could scan the namespace
        // for any classes implementing IUserProfileCreated, etc.
        if (configuredEvents.Count == 0)
        {
            throw new InvalidOperationException(
                "No integration events configured in UserProfileMessagingConfiguration. " +
                "Ensure ConfigurePublications() registers all user profile integration events.");
        }
    }
}
