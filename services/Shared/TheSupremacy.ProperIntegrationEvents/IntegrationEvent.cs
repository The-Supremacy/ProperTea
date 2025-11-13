namespace TheSupremacy.ProperIntegrationEvents;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredAt, Guid? CorrelationId = null)
{
    public abstract string EventType { get; }
}