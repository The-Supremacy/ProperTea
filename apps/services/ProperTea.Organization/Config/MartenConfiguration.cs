using JasperFx;
using JasperFx.Events;
using Marten;
using Npgsql;
using ProperTea.Organization.Features.Organizations;
using Wolverine.Marten;

namespace ProperTea.Organization.Config;

/// <summary>
/// Cross-cutting Marten infrastructure configuration.
/// Feature-specific projections and events are configured in their respective feature folders.
/// </summary>
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

            // Cross-cutting policies
            _ = opts.Policies.AllDocumentsAreMultiTenanted();
            opts.Events.TenancyStyle = Marten.Storage.TenancyStyle.Conjoined;
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.EventNamingStyle = EventNamingStyle.SmarterTypeName;
            opts.Events.AppendMode = EventAppendMode.Quick;
            opts.Events.UseMandatoryStreamTypeDeclaration = true;
            opts.Events.UseArchivedStreamPartitioning = true;

            opts.DatabaseSchemaName = "organization";
            opts.AutoCreateSchemaObjects = environment.IsDevelopment()
                ? AutoCreate.All
                : AutoCreate.CreateOrUpdate;

            opts.Projections.UseIdentityMapForAggregates = true;

            // Feature-specific configurations
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
