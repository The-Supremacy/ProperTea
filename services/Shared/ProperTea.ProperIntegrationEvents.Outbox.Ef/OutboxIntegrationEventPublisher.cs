using System.Text.Json;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public class OutboxIntegrationEventPublisher(IOutboxDbContext dbContext) : IIntegrationEventPublisher
{
    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = @event.Id,
            OccurredAt = @event.OccurredAt,
            Topic = topic,
            EventType = @event.EventType,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            Status = OutboxMessageStatus.Pending,
        };

        await dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
    }
}