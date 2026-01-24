using JasperFx;
using JasperFx.Events;
using Marten;
using Npgsql;
using ProperTea.Organization.Features.Organizations.Configuration;
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
                ?? throw new InvalidOperationException("Connection string 'organization-db' not found");
            opts.Connection(connectionString);

            _ = opts.Policies.AllDocumentsAreMultiTenanted();
            opts.Events.TenancyStyle = Marten.Storage.TenancyStyle.Conjoined;
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.EventNamingStyle = EventNamingStyle.SmarterTypeName;
            opts.Events.AppendMode = EventAppendMode.Quick;
            opts.Events.UseMandatoryStreamTypeDeclaration = true;
            opts.Events.UseArchivedStreamPartitioning = true;

            opts.Events.MetadataConfig.UserNameEnabled = true;
            opts.Events.MetadataConfig.CorrelationIdEnabled = true;
            opts.Events.MetadataConfig.CausationIdEnabled = true;

            opts.DatabaseSchemaName = "organization";
            opts.AutoCreateSchemaObjects = environment.IsDevelopment()
                ? AutoCreate.All
                : AutoCreate.CreateOrUpdate;

            opts.Projections.UseIdentityMapForAggregates = true;

            opts.ConfigureOrganizationMarten();
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
