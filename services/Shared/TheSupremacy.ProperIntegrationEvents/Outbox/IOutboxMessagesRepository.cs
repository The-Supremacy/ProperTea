namespace TheSupremacy.ProperIntegrationEvents.Outbox;

public interface IOutboxMessagesRepository
{
    Task SaveAsync(OutboxMessage message, CancellationToken ct);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken ct);
    Task<OutboxMessage?> GetByIdAsync(Guid id);
}