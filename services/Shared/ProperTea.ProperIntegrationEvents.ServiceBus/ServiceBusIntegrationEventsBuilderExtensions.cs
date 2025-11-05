using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.ServiceBus;

public static class ServiceBusIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder UseServiceBus(this IntegrationEventsBuilder builder, string connectionString)
    {
        builder.Services.AddSingleton<ServiceBusClient>(provider => new ServiceBusClient(connectionString));
        builder.Services
            .TryAddSingleton<IExternalIntegrationEventPublisher, ServiceBusExternalIntegrationEventPublisher>();

        return builder;
    }
}