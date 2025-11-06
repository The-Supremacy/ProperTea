namespace ProperTea.ProperIntegrationEvents.Outbox;

public interface IOutboxMessagesPublisher : IIntegrationEventPublisher
{
    public Task SaveMessageAsync(OutboxMessage message, CancellationToken ct = default);
}

public interface IOutboxMessagesReader
{
    public Task<IEnumerable<OutboxMessage>> GetPendingOutboxMessagesAsync(
        int batchSize = 10, CancellationToken ct = default);

    public Task<OutboxMessage?> GetMessageByIdAsync(Guid id);
}

public interface IOutboxMessagesService : IOutboxMessagesReader, IOutboxMessagesPublisher
{
}