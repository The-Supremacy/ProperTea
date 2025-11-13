using TheSupremacy.ProperDomain;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests.Setup;

public record TestDomainEvent(Guid AggregateId, Guid EventId, DateTime OccurredAt) : IDomainEvent;

public class TestAggregate : AggregateRoot
{
    public TestAggregate() : base(Guid.NewGuid())
    {
    }

    public TestAggregate(string name) : base(Guid.NewGuid())
    {
        Name = name;
    }

    public string? Name { get; private set; }

    public void ChangeName(string newName)
    {
        Name = newName;
    }

    public void DoSomething()
    {
        RaiseDomainEvent(new TestDomainEvent(Id, Guid.NewGuid(), DateTime.UtcNow));
    }
}