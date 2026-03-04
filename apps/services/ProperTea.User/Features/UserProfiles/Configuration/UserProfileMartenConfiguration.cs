using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk.Kiota;
using Marten;
using Marten.Events.Projections;
using ProperTea.User.Features.UserProfiles.Infrastructure;

namespace ProperTea.User.Features.UserProfiles.Configuration;

public static class UserProfileConfiguration
{
    public static IServiceCollection AddUserProfileFeature(
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
            .AddClient("keycloak-admin-user", client =>
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
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak-admin-user"));

        _ = services.AddTransient<KeycloakUserClient>();
        _ = services.AddTransient<IExternalUserClient>(
            sp => sp.GetRequiredService<KeycloakUserClient>());

        return services;
    }

    public static void ConfigureUserProfileMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<UserProfileAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<UserProfileAggregate>()
            .Index(x => x.UserId, idx => idx.IsUnique = true);
    }
}

