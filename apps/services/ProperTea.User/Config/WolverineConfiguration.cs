using JasperFx.Resources;
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
                .AutoProvision();

            _ = opts.ListenToRabbitQueue("user.commands")
                .UseDurableInbox();

            _ = opts.Services.AddResourceSetupOnStartup();

            opts.Policies.UseDurableLocalQueues();
            opts.Policies.AutoApplyTransactions();
        });

        return builder;
    }
}
