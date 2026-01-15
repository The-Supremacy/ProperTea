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

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .EnableWolverineControlQueues()
                .AutoProvision();

            _ = opts.Services.AddResourceSetupOnStartup();

            opts.Policies.UseDurableLocalQueues();
            opts.Policies.AutoApplyTransactions();

            opts.ConfigureOrganizationMessaging();

#if DEBUG
            OrganizationMessagingConfiguration.ValidateConfiguration();
#endif
        });

        return builder;
    }
}
