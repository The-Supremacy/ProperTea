using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperIntegrationEvents;

public class IntegrationEventsBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
    public Dictionary<string, Type> EventTypes { get; } = new();

    public IntegrationEventsBuilder AddEventType<TEvent>(string eventType)
        where TEvent : IntegrationEvent
    {
        EventTypes[eventType] = typeof(TEvent);
        return this;
    }
}