namespace ProperTea.ProperIntegrationEvents.Outbox;

public interface IOutboxMessagesService
{
    public Task<IEnumerable<OutboxMessage>> GetPendingOutboxMessagesAsync(
        int batchSize = 10, CancellationToken ct = default);

    public Task SaveMessageAsync(OutboxMessage message, CancellationToken ct = default);
}