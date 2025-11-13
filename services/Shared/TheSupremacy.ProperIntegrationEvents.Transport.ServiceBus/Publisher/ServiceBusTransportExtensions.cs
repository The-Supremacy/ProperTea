using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheSupremacy.ProperIntegrationEvents.Outbox;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Publisher;

public static class ServiceBusTransportExtensions
{
    public static OutboxBuilder AddServiceBusTransport(
        this OutboxBuilder builder,
        Action<ServiceBusPublisherConfiguration> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.TryAddSingleton<IMessageTransport, ServiceBusMessageTransport>();
        
        builder.Services.AddHealthChecks()
            .AddCheck<ServiceBusHealthCheck>("servicebus", tags: ["transport"]);
        return builder;
    }

    public static OutboxBuilder AddServiceBusTransport(
        this OutboxBuilder builder,
        string connectionString)
    {
        return builder.AddServiceBusTransport(config =>
        {
            config.ConnectionString = connectionString;
        });
    }
}