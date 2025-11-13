namespace TheSupremacy.ProperDomain.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent(Guid EventId, DateTime OccurredAt) : IDomainEvent;