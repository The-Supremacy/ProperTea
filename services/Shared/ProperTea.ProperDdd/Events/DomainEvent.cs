namespace ProperTea.ProperDdd.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public interface IPrioritizedDomainEvent : IDomainEvent
{
    int Priority { get; }
}

public abstract record DomainEvent(Guid EventId, DateTime OccurredAt) : IDomainEvent;