using Wolverine;
using ProperTea.Organization.Extensions;

namespace ProperTea.Organization.Features.Organizations.Configuration;

public static class OrganizationMessagingConfiguration
{
    public static void ConfigureOrganizationIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationRegistered>(
            "organization.events");

        opts.PublishIntegrationEvent<OrganizationIntegrationEvents.OrganizationActivated>(
            "organization.events");
    }
}
