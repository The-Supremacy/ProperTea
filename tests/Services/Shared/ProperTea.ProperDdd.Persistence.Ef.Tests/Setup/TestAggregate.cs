using ProperTea.ProperDdd.Events;

namespace ProperTea.ProperDdd.Persistence.Ef.Tests.Setup;

public record TestDomainEvent(Guid AggregateId, Guid EventId, DateTime OccurredAt) : IDomainEvent;

public class TestAggregate : AggregateRoot
{
    public string? Name { get; private set; } 
    
    public TestAggregate() : base(Guid.NewGuid())
    {
    }
    
    public TestAggregate(string name) : base(Guid.NewGuid())
    {
        Name = name;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
    }
    
    public void DoSomething()
    {
        RaiseDomainEvent(new TestDomainEvent(Id, Guid.NewGuid(), DateTime.UtcNow));
    }
}