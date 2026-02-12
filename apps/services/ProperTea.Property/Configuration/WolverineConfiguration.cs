using JasperFx;
using JasperFx.Core;
using JasperFx.Resources;
using ProperTea.Property.Features.Properties.Configuration;
using ProperTea.Property.Features.Units.Configuration;
using ProperTea.Infrastructure.Common.Auth;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.RabbitMQ;

namespace ProperTea.Property.Configuration;

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
            opts.Policies.AddMiddleware<UserIdMiddleware>();

            opts.UnknownMessageBehavior = UnknownMessageBehavior.DeadLetterQueue;

            _ = opts.UseRabbitMqUsingNamedConnection("rabbitmq")
                .DeclareExchange("company.events", exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    _ = exchange.BindQueue("property.company-events", "companies.#");
                })
                .EnableWolverineControlQueues()
                .AutoProvision();

            _ = opts.ListenToRabbitQueue("property.company-events").UseDurableInbox();

            opts.ConfigurePropertyIntegrationEvents();
            opts.ConfigureUnitIntegrationEvents();

            _ = opts
                .OnException<ConcurrencyException>()
                .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds(), 500.Milliseconds())
                .Then.MoveToErrorQueue();

            _ = opts.Services.AddWolverineHttp();
        });

        return builder;
    }
}
