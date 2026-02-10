using Marten;
using Marten.Events.Projections;
using ProperTea.User.Features.UserProfiles.Infrastructure;
using Zitadel.Credentials;

namespace ProperTea.User.Features.UserProfiles.Configuration;

public static class UserProfileConfiguration
{
    public static IServiceCollection AddUserProfileFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var serviceAccountJwtPath = config["Zitadel:ServiceAccountJwtPath"]
                ?? throw new InvalidOperationException("Zitadel:ServiceAccountJwtPath not configured");

            return ServiceAccount.LoadFromJsonFile(serviceAccountJwtPath);
        });

        _ = services.AddSingleton<IExternalUserClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<ZitadelUserClient>>();
            var serviceAccount = sp.GetRequiredService<ServiceAccount>();

            var apiUrl = config["OIDC:Authority"]
                ?? throw new InvalidOperationException("OIDC:Authority not configured");

            var allowInsecure = environment.IsDevelopment();

            return new ZitadelUserClient(apiUrl, serviceAccount, logger, allowInsecure);
        });

        return services;
    }

    public static void ConfigureUserProfileMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<UserProfileAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<UserProfileAggregate>()
            .Index(x => x.ExternalUserId, idx => idx.IsUnique = true);
    }
}
