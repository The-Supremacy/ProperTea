using JasperFx.Resources;
using ProperTea.User.Features.UserProfiles.Configuration;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

namespace ProperTea.User.Config;

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
                .DeclareExchange("organization.events", exchange =>
                {
                    _ = exchange.BindQueue("user.organization-events");
                })
                .EnableWolverineControlQueues()
                .AutoProvision();
            _ = opts.ListenToRabbitQueue("user.organization-events").UseDurableInbox();

            opts.ConfigureUserProfileIntegrationEvents();
        });

        return builder;
    }
}
