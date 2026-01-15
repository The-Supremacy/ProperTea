using JasperFx.Resources;
using ProperTea.Organization.Features.Organizations;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Config;

/// <summary>
/// Infrastructure-level Wolverine configuration.
/// Feature-specific messaging configuration is in feature folders.
/// </summary>
public static class WolverineConfiguration
{
    public static IHostBuilder AddWolverineConfiguration(
        this IHostBuilder builder)
    {
        _ = builder.UseWolverine(opts =>
        {
            // === INFRASTRUCTURE ===
            _ = opts.ApplicationAssembly = typeof(Program).Assembly;
            _ = opts.UseFluentValidation();

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .AutoProvision();

            _ = opts.Services.AddResourceSetupOnStartup();

            // === GLOBAL POLICIES ===
            // Internal commands use durable local queues (transactional with Marten)
            opts.Policies.UseDurableLocalQueues();

            // Automatically enlist Marten sessions in transactions
            opts.Policies.AutoApplyTransactions();

            // === FEATURE MESSAGING (Explicit) ===
            // All external message pub/sub configured explicitly per feature
            opts.ConfigureOrganizationMessaging();

#if DEBUG
            // Validate messaging configuration at startup (Debug only)
            OrganizationMessagingConfiguration.ValidateConfiguration();
#endif
        });

        return builder;
    }
}
