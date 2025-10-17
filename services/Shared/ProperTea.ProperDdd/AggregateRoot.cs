using ProperTea.ProperDdd.Events;

namespace ProperTea.ProperDdd;

public interface IAggregateRoot
{
    public Guid Id { get; }
    
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

public abstract class AggregateRoot(Guid id) : Entity(id), IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private AggregateRoot() : this(Guid.Empty)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}