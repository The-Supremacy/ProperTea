using JasperFx;
using JasperFx.Events;
using Marten;
using Npgsql;
using ProperTea.User.Features.UserProfiles;
using ProperTea.User.Features.UserProfiles.Configuration;
using ProperTea.User.Features.UserPreferences.Configuration;
using Wolverine.Marten;

namespace ProperTea.User.Config;

public static class MartenConfiguration
{
    public static IServiceCollection AddMartenConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddMarten(opts =>
        {
            var connectionString = configuration.GetConnectionString("user-db")
                ?? throw new InvalidOperationException("Connection string 'user-db' not found");
            opts.Connection(connectionString);

            // Cross-cutting policies
            _ = opts.Policies.AllDocumentsAreMultiTenanted();
            opts.Events.TenancyStyle = Marten.Storage.TenancyStyle.Conjoined;
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.EventNamingStyle = EventNamingStyle.SmarterTypeName;
            opts.Events.AppendMode = EventAppendMode.Quick;
            opts.Events.UseMandatoryStreamTypeDeclaration = true;
            opts.Events.UseArchivedStreamPartitioning = true;

            opts.DatabaseSchemaName = "user_profile";
            opts.AutoCreateSchemaObjects = environment.IsDevelopment()
                ? AutoCreate.All
                : AutoCreate.CreateOrUpdate;

            // Explicit event type mapping (stable names independent of namespace/assembly)
            // Convention: {aggregate}.{event-name}.v{version}
            opts.Events.MapEventType<UserProfileEvents.Created>("user-profile.created.v1");
            opts.Events.MapEventType<UserProfileEvents.LastSeenUpdated>("user-profile.last-seen-updated.v1");
            opts.Events.MapEventType<UserProfileEvents.OrganizationDeactivatedMarked>("user-profile.org-deactivated-marked.v1");
            opts.Events.MapEventType<UserProfileEvents.OrganizationDeactivatedCleared>("user-profile.org-deactivated-cleared.v1");

            // Feature-specific configurations
            opts.ConfigureUserProfileMarten();
            opts.ConfigureUserPreferencesMarten();
        })
        .UseLightweightSessions()
        .IntegrateWithWolverine(
            cfg =>
            {
                cfg.UseWolverineManagedEventSubscriptionDistribution = true;
            });

        _ = services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddNpgsql());

        return services;
    }
}
