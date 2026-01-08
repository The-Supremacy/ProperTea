using System.Globalization;
using JasperFx.Resources;
using ProperTea.Organization.Features;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Config;

public static class WolverineConfiguration
{
    public static IHostBuilder AddWolverineConfiguration(
        this IHostBuilder builder)
    {
        _ = builder.UseWolverine(opts =>
        {
            _ = opts.ApplicationAssembly = typeof(Program).Assembly;

            _ = opts.UseFluentValidation();

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .AutoProvision();

            _ = opts.PublishMessagesToRabbitMqExchange<IIntegrationEvent>(
                "organization.events",
                m => m.GetType().Name.ToLower(CultureInfo.InvariantCulture))
                .UseDurableOutbox();

            _ = opts.ListenToRabbitQueue("organization.commands")
                .UseDurableInbox();

            _ = opts.Services.AddResourceSetupOnStartup();

            opts.Policies.UseDurableLocalQueues();
            opts.Policies.AutoApplyTransactions();
        });

        return builder;
    }
}
