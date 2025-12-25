using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using Marten;
using Npgsql;
using Wolverine.Marten;

namespace ProperTea.Organization.Config;

public static class MartenConfiguration
{
    public static IServiceCollection AddMartenConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddMarten(opts =>
        {
            var connectionString = configuration.GetConnectionString("organization-db")
        ?? throw new InvalidOperationException("Postgres connection string not configured");

            opts.Connection(connectionString);

            _ = opts.Policies.AllDocumentsAreMultiTenanted();
            opts.Events.TenancyStyle = Marten.Storage.TenancyStyle.Conjoined;

            _ = opts.Policies.AllDocumentsSoftDeleted();

            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.UseArchivedStreamPartitioning = true;


            opts.Projections.UseIdentityMapForAggregates = true;

            opts.DatabaseSchemaName = "organization";
            opts.AutoCreateSchemaObjects = environment.IsDevelopment()
                ? AutoCreate.All
                : AutoCreate.CreateOrUpdate;

            // Projections will be added here as we build features
            // opts.Projections.Add<OrganizationListProjection>(ProjectionLifecycle.Async);
        })
        .UseLightweightSessions()
        .AddAsyncDaemon(DaemonMode.HotCold)
        .IntegrateWithWolverine();

        _ = services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddNpgsql());

        return services;
    }
}
