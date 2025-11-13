using System.Collections.Concurrent;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.IntegrationTests.TestHelpers;

public class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
{
    private readonly ConcurrentBag<TestIntegrationEvent> _processedEvents = [];

    public IReadOnlyCollection<TestIntegrationEvent> ProcessedEvents => _processedEvents;

    public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        _processedEvents.Add(integrationEvent);
        return Task.CompletedTask;
    }
}