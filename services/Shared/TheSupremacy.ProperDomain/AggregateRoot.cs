using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain;

public interface IAggregateRoot
{
    public Guid Id { get; }

    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

public abstract class AggregateRoot(Guid id) : Entity(id), IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    internal AggregateRoot() : this(Guid.Empty)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}