using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheSupremacy.ProperIntegrationEvents.Outbox;

namespace TheSupremacy.ProperIntegrationEvents.Persistence.Ef;

public class DbContextOutboxMessagesService(IOutboxDbContext dbContext) : IIntegrationEventPublisher, IOutboxMessagesRepository
{
    public async Task PublishAsync<TEvent>(string topic, TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = integrationEvent.Id,
            OccurredAt = integrationEvent.OccurredAt,
            Topic = topic,
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            Status = OutboxMessageStatus.Pending
        };

        await dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 10, CancellationToken ct = default)
    {
        return await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending &&
                        (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id)
    {
        return await dbContext.OutboxMessages.FindAsync(id);
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken ct)
    {
        dbContext.OutboxMessages.Update(message);
        await ((DbContext)dbContext).SaveChangesAsync(ct);
    }
}