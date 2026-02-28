using Keycloak.AuthServices.Sdk;
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
        services.AddDistributedMemoryCache();

        _ = services.AddKeycloakAdminHttpClient(configuration.GetSection("Keycloak"))
            .AddTypedClient<KeycloakUserClient>();

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
