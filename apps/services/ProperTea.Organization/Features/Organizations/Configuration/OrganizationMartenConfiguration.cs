using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk.Kiota;
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
        _ = services.AddDistributedMemoryCache();

        // Acquire admin tokens via client_credentials so the Kiota admin client can call Keycloak.
        var authServerUrl = configuration["Keycloak:AuthServerUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Keycloak:AuthServerUrl not configured");
        var realm = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak:Realm not configured");

        _ = services.AddClientCredentialsTokenManagement()
            .AddClient("keycloak-admin-org", client =>
            {
                client.TokenEndpoint = new Uri($"{authServerUrl}/realms/{realm}/protocol/openid-connect/token");
                client.ClientId = ClientId.Parse(
                    configuration["Keycloak:Resource"]
                    ?? throw new InvalidOperationException("Keycloak:Resource not configured"));
                client.ClientSecret = ClientSecret.Parse(
                    configuration["Keycloak:Credentials:Secret"]
                    ?? throw new InvalidOperationException("Keycloak:Credentials:Secret not configured"));
            });

        _ = services.AddKiotaKeycloakAdminHttpClient(configuration)
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak-admin-org"));

        _ = services.AddTransient<KeycloakOrganizationClient>();
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

