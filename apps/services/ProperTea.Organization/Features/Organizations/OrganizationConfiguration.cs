using Marten;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using Zitadel.Credentials;

namespace ProperTea.Organization.Features.Organizations;

/// <summary>
/// Feature-specific configuration for the Organizations slice.
/// Registers all dependencies, Marten projections, and event types owned by this feature.
/// </summary>
public static class OrganizationConfiguration
{
    /// <summary>
    /// Registers all services required by the Organizations feature.
    /// </summary>
    public static IServiceCollection AddOrganizationFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddSingleton<IZitadelClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<ZitadelClient>>();

            var apiUrl = config["OIDC:Authority"]
                ?? throw new InvalidOperationException("OIDC:Authority not configured");

            var serviceAccountPath = config["Zitadel:ServiceAccountPath"]
                ?? throw new InvalidOperationException("Zitadel:ServiceAccountPath not configured");

            var serviceAccount = ServiceAccount.LoadFromJsonFile(serviceAccountPath);

            // Allow HTTP in development (ZITADEL runs without TLS locally)
            var allowInsecure = environment.IsDevelopment();

            return new ZitadelClient(apiUrl, serviceAccount, logger, allowInsecure);
        });

        return services;
    }

    /// <summary>
    /// Configures Marten projections and event types for the Organizations feature.
    /// Called from the central MartenConfiguration.
    /// </summary>
    public static void ConfigureOrganizationMarten(this StoreOptions opts)
    {
        // Direct aggregate snapshot using Apply methods via reflection
        _ = opts.Projections.Snapshot<OrganizationAggregate>(SnapshotLifecycle.Inline);

        // Event types
        // Explicit event type mapping (stable names independent of namespace/assembly)
        // Convention: {aggregate}.{event-name}.v{version}
        opts.Events.MapEventType<OrganizationEvents.Created>("organization.created.v1");
        opts.Events.MapEventType<OrganizationEvents.ZitadelOrganizationCreated>("organization.zitadel-linked.v1");
        opts.Events.MapEventType<OrganizationEvents.Activated>("organization.activated.v1");
        opts.Events.MapEventType<OrganizationEvents.NameChanged>("organization.name-changed.v1");
        opts.Events.MapEventType<OrganizationEvents.SlugChanged>("organization.slug-changed.v1");
        opts.Events.MapEventType<OrganizationEvents.Deactivated>("organization.deactivated.v1");
        opts.Events.MapEventType<OrganizationEvents.DomainAdded>("organization.domain-added.v1");
        opts.Events.MapEventType<OrganizationEvents.DomainVerified>("organization.domain-verified.v1");
    }
}
