using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public static class ServiceBusConsumerExtensions
{
    public static IntegrationEventsBuilder AddServiceBusConsumer(
        this IntegrationEventsBuilder builder,
        Action<ServiceBusConsumerConfiguration> configure)
    {
        builder.Services.Configure(configure);

        // Register processor as scoped (for handler DI)
        builder.Services.TryAddScoped<IServiceBusMessageProcessor, ServiceBusMessageProcessor>();

        // Register consumer as singleton (long-lived)
        builder.Services.TryAddSingleton<ServiceBusIntegrationEventConsumer>();

        return builder;
    }

    public static IntegrationEventsBuilder AddEventHandler<TEvent, THandler>(
        this IntegrationEventsBuilder builder)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        if (typeof(TEvent).GetProperty(nameof(IntegrationEvent.EventType))
                ?.GetValue(null) is string eventTypeName && !builder.EventTypes.ContainsKey(eventTypeName))
        {
            throw new InvalidOperationException(
                $"Event type '{typeof(TEvent).Name}' must be registered with AddEventType() before adding handlers");
        }
        
        builder.Services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        return builder;
    }
}