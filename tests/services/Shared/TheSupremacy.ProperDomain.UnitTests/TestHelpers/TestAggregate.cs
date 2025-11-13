namespace TheSupremacy.ProperDomain.UnitTests.TestHelpers;

public class TestAggregate : AggregateRoot
{
    public TestAggregate(Guid id) : base(id) 
    {
    }

    private TestAggregate() : base()
    {
    }
    
    public void DoSomething()
    {
        RaiseDomainEvent(new TestAggregateDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id));
    }
}