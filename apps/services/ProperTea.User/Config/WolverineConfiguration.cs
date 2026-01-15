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

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .DeclareExchange("organization.events", exchange =>
                {
                    // Fanout exchange - all bound queues receive all messages
                    exchange.ExchangeType = ExchangeType.Fanout;
                    exchange.BindQueue("user.organization-events");
                })
                .EnableWolverineControlQueues()
                .AutoProvision();

            _ = opts.Services.AddResourceSetupOnStartup();

            opts.Policies.UseDurableLocalQueues();
            opts.Policies.AutoApplyTransactions();

            opts.ConfigureUserProfileMessaging();

#if DEBUG
            UserProfileMessagingConfiguration.ValidateConfiguration();
#endif
        });

        return builder;
    }
}
