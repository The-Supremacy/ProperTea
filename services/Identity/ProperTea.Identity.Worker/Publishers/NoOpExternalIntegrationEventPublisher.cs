using ProperTea.ProperIntegrationEvents;

namespace ProperTea.Identity.Worker.Publishers;

/// <summary>
/// Temporary no-op publisher that just logs events.
/// TODO: Replace with actual RabbitMQ or Azure Service Bus publisher
/// </summary>
public class NoOpExternalIntegrationEventPublisher : IExternalIntegrationEventPublisher
{
    private readonly ILogger<NoOpExternalIntegrationEventPublisher> _logger;

    public NoOpExternalIntegrationEventPublisher(ILogger<NoOpExternalIntegrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        _logger.LogInformation(
            "NoOp Publisher: Would publish event {EventType} to topic {Topic}. EventId: {EventId}",
            @event.GetType().Name,
            topic,
            @event.Id);
        
        return Task.CompletedTask;
    }
}

