using JasperFx.Resources;
using ProperTea.Organization.Features.Organizations.Configuration;
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

            _ = opts.Services.AddResourceSetupOnStartup();

            opts.Policies.UseDurableLocalQueues();
            opts.Policies.AutoApplyTransactions();

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .DeclareExchange("user.events", exchange =>
                {
                    _ = exchange.BindQueue("organization.user-events");
                })
                .EnableWolverineControlQueues()
                .AutoProvision();
            _ = opts.ListenToRabbitQueue("organization.user-events").UseDurableInbox();

            opts.ConfigureOrganizationIntegrationEvents();
        });

        return builder;
    }
}
