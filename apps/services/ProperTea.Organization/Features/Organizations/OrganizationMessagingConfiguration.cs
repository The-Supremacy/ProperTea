using Wolverine;
using ProperTea.Organization.Config;

namespace ProperTea.Organization.Features.Organizations;

/// <summary>
/// Explicit configuration for Organization feature messaging.
/// All integration event publications and subscriptions are defined here.
/// No auto-discovery - every external message must be explicitly configured.
/// </summary>
public static class OrganizationMessagingConfiguration
{
    public static void ConfigureOrganizationMessaging(this WolverineOptions opts)
    {
        ConfigurePublications(opts);
        ConfigureSubscriptions(opts);
    }

    /// <summary>
    /// Configure outgoing integration events.
    /// Convention: organizations.{event-name}.v{version}
    /// </summary>
    private static void ConfigurePublications(WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationRegistered>(
            "organization.events",
            "organizations.registered.v1");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationIdentityUpdated>(
            "organization.events",
            "organizations.identity-updated.v1");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationDeactivated>(
            "organization.events",
            "organizations.deactivated.v1");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationDomainVerified>(
            "organization.events",
            "organizations.domain-verified.v1");
    }

    /// <summary>
    /// Configure incoming message subscriptions.
    /// Explicitly define which external events this service consumes.
    /// </summary>
    private static void ConfigureSubscriptions(WolverineOptions opts)
    {
        // Example: If Organization service needs to listen to user events
        // opts.ListenToRabbitQueue("organization.user-events")
        //     .UseDurableInbox()
        //     .Named("UserProfileSubscriptions");

        // For now, Organization service doesn't consume external events
        // Add subscriptions here when needed
    }

    /// <summary>
    /// Validates that all integration events are properly configured.
    /// Called on startup to fail fast if configuration is incomplete.
    /// </summary>
    public static void ValidateConfiguration()
    {
        // Get all nested integration event types
        var integrationEventTypes = typeof(OrganizationIntegrationEvents)
            .GetNestedTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var expectedEventNames = new HashSet<string>(integrationEventTypes.Select(t => t.Name));
        var configuredEvents = new HashSet<string>
        {
            nameof(OrganizationIntegrationEvents.OrganizationRegistered),
            nameof(OrganizationIntegrationEvents.OrganizationIdentityUpdated),
            nameof(OrganizationIntegrationEvents.OrganizationDeactivated),
            nameof(OrganizationIntegrationEvents.OrganizationDomainVerified)
        };

        var missingEvents = expectedEventNames.Except(configuredEvents).ToList();

        if (missingEvents.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing Wolverine configuration for integration events: {string.Join(", ", missingEvents)}. " +
                "Ensure all events are registered in OrganizationMessagingConfiguration.ConfigurePublications().");
        }
    }
}
