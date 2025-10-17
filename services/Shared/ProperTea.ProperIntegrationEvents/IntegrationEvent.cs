namespace ProperTea.ProperIntegrationEvents;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredAt)
{
    public abstract string EventType { get; }
}