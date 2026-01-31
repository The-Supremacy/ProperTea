using JasperFx;
using JasperFx.Events;
using Marten;
using Npgsql;
using ProperTea.Company.Features.Companies.Configuration;
using Wolverine.Marten;

namespace ProperTea.Company.Config;

public static class MartenConfiguration
{
    public static IServiceCollection AddMartenConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddMarten(opts =>
        {
            var connectionString = configuration.GetConnectionString("company-db")
                ?? throw new InvalidOperationException("Connection string 'company-db' not found");
            opts.Connection(connectionString);

            // Multi-tenancy configuration
            _ = opts.Policies.AllDocumentsAreMultiTenanted();
            opts.Events.TenancyStyle = Marten.Storage.TenancyStyle.Conjoined;
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.EventNamingStyle = EventNamingStyle.SmarterTypeName;
            opts.Events.AppendMode = EventAppendMode.Quick;
            opts.Events.UseMandatoryStreamTypeDeclaration = true;
            opts.Events.UseArchivedStreamPartitioning = true;

            // Event metadata
            opts.Events.MetadataConfig.UserNameEnabled = true;
            opts.Events.MetadataConfig.CorrelationIdEnabled = true;
            opts.Events.MetadataConfig.CausationIdEnabled = true;

            opts.DatabaseSchemaName = "company";
            opts.AutoCreateSchemaObjects = environment.IsDevelopment()
                ? AutoCreate.All
                : AutoCreate.CreateOrUpdate;

            opts.Projections.UseIdentityMapForAggregates = true;

            opts.ConfigureCompanyMarten();
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
