namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.IntegrationTests.TestHelpers;

public record TestIntegrationEvent(Guid Id, DateTime OccurredAt, Guid? CorrelationId = null)
    : IntegrationEvent(Id, OccurredAt, CorrelationId)
{
    public override string EventType => "TestEvent";
    public string? TestData { get; init; }
}