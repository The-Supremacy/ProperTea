using Marten;
using Marten.Events.Projections;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using Zitadel.Credentials;

namespace ProperTea.Organization.Features.Organizations.Configuration;

public static class OrganizationConfiguration
{
    public static IServiceCollection AddOrganizationFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddSingleton<IExternalOrganizationClient>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<ZitadelOrganizationClient>>();

                var apiUrl = config["OIDC:Authority"]
                    ?? throw new InvalidOperationException("OIDC:Authority not configured");

                var serviceAccountPath = config["Zitadel:ServiceAccountPath"]
                    ?? throw new InvalidOperationException("Zitadel:ServiceAccountPath not configured");

                var serviceAccount = ServiceAccount.LoadFromJsonFile(serviceAccountPath);

                var allowInsecure = environment.IsDevelopment();

                return new ZitadelOrganizationClient(apiUrl, serviceAccount, logger, allowInsecure);
            });

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
