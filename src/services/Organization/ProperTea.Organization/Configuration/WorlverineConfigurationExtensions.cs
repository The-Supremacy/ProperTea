using JasperFx.CodeGeneration;
using Microsoft.EntityFrameworkCore;
using ProperTea.Organization.Persistence;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Configuration;

public static class WorlverineConfigurationExtensions
{
    public static void ConfigureWolverine(this WolverineOptions opts, WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database")!;

        opts.UseFluentValidation();

        // EF Core Integration
        builder.Services.AddDbContextWithWolverineIntegration<OrganizationDbContext>(
            x => x.UseNpgsql(connectionString));

        opts.PersistMessagesWithPostgresql(connectionString);
        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.AutoApplyTransactions();

        // Durability Policies
        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
        opts.Policies.UseDurableInboxOnAllListeners();
        opts.Policies.UseDurableLocalQueues();

        // Discover Sagas
        opts.Discovery.IncludeAssembly(typeof(Program).Assembly); // Auto-discover sagas/handlers in this project

        // Transport Configuration
        if (builder.Environment.IsDevelopment())
        {
            var rabbitMqConn = builder.Configuration.GetConnectionString("RabbitMq")!;
            opts.UseRabbitMq(rabbitMqConn).AutoProvision();

            opts.Durability.Mode = DurabilityMode.Solo;
            opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
        }
        else
        {
            var serviceBusConn = builder.Configuration.GetConnectionString("ServiceBus")!;
            opts.UseAzureServiceBus(serviceBusConn).AutoProvision();

            opts.Durability.Mode = DurabilityMode.Balanced;
            opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
        }
    }
}
