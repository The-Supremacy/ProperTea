using Wolverine;
using ProperTea.Organization.Extensions;

namespace ProperTea.Organization.Features.Organizations.Configuration;

public static class OrganizationMessagingConfiguration
{
    public static void ConfigureOrganizationMessaging(this WolverineOptions opts)
    {
        ConfigurePublications(opts);
        ConfigureSubscriptions(opts);
    }

    private static void ConfigurePublications(WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationRegistered>(
            "organization.events");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationIdentityUpdated>(
            "organization.events");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationDeactivated>(
            "organization.events");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationActivated>(
            "organization.events");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationDomainVerified>(
            "organization.events");
    }

    private static void ConfigureSubscriptions(WolverineOptions opts)
    {
        // Example: If Organization service needs to listen to user events
        // opts.ListenToRabbitQueue("organization.user-events")
        //     .UseDurableInbox()
        //     .Named("UserProfileSubscriptions");

        // For now, Organization service doesn't consume external events
        // Add subscriptions here when needed
    }

    public static void ValidateConfiguration()
    {
        var integrationEventTypes = typeof(OrganizationIntegrationEvents)
            .GetNestedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var expectedEventNames = new HashSet<string>(integrationEventTypes.Select(t => t.Name));
        var configuredEvents = new HashSet<string>
        {
            nameof(OrganizationIntegrationEvents.OrganizationRegistered),
            nameof(OrganizationIntegrationEvents.OrganizationIdentityUpdated),
            nameof(OrganizationIntegrationEvents.OrganizationDeactivated),
            nameof(OrganizationIntegrationEvents.OrganizationActivated),
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
