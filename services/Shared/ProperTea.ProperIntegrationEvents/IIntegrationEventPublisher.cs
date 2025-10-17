namespace ProperTea.ProperIntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent;
}

public interface IExternalIntegrationEventPublisher : IIntegrationEventPublisher
{
}