using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents;

public class IntegrationEventsBuilder
{
    public IntegrationEventsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
    public Dictionary<string, Type> EventTypes { get; } = new();

    public IntegrationEventsBuilder AddEventType<TEvent>(string eventType)
        where TEvent : IntegrationEvent
    {
        EventTypes[eventType] = typeof(TEvent);
        return this;
    }
}