using Keycloak.AuthServices.Sdk;
using Marten;
using Marten.Events.Projections;
using ProperTea.Organization.Infrastructure;

namespace ProperTea.Organization.Features.Organizations.Configuration;

public static class OrganizationConfiguration
{
    public static IServiceCollection AddOrganizationFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDistributedMemoryCache();

        _ = services.AddKeycloakAdminHttpClient(configuration.GetSection("Keycloak"))
            .AddTypedClient<KeycloakOrganizationClient>();

        _ = services.AddTransient<IExternalOrganizationClient>(
            sp => sp.GetRequiredService<KeycloakOrganizationClient>());

        return services;
    }

    public static void ConfigureOrganizationMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<OrganizationAggregate>(SnapshotLifecycle.Inline);
        _ = opts.Schema.For<OrganizationAggregate>()
            .Index(x => x.OrganizationId, idx =>
            {
                idx.IsUnique = true;
            });

        // Convention: {aggregate}.{event-name}.v{version}
        opts.Events.MapEventType<OrganizationEvents.Created>("organization.created.v1");
        opts.Events.MapEventType<OrganizationEvents.OrganizationLinked>("organization.linked.v2");
        opts.Events.MapEventType<OrganizationEvents.Activated>("organization.activated.v1");
    }
}
