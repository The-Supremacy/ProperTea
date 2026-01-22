using Marten;
using Marten.Events.Projections;

namespace ProperTea.Organization.Features.Organizations.Configuration;

public static class OrganizationConfiguration
{
    public static IServiceCollection AddOrganizationFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        return services;
    }

    public static void ConfigureOrganizationMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<OrganizationAggregate>(SnapshotLifecycle.Inline);

        // Convention: {aggregate}.{event-name}.v{version}
        opts.Events.MapEventType<OrganizationEvents.Created>("organization.created.v1");
        opts.Events.MapEventType<OrganizationEvents.ExternalOrganizationCreated>("organization.external-linked.v1");
        opts.Events.MapEventType<OrganizationEvents.Activated>("organization.activated.v1");
        opts.Events.MapEventType<OrganizationEvents.NameChanged>("organization.name-changed.v1");
        opts.Events.MapEventType<OrganizationEvents.SlugChanged>("organization.slug-changed.v1");
        opts.Events.MapEventType<OrganizationEvents.Deactivated>("organization.deactivated.v1");
    }
}
