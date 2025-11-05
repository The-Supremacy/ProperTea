using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.ServiceBus;

public static class ServiceBusIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder UseServiceBus(this IntegrationEventsBuilder builder)
    {
        builder.Services
            .TryAddSingleton<IExternalIntegrationEventPublisher, ServiceBusExternalIntegrationEventPublisher>();

        return builder;
    }
}