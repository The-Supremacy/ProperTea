using Shouldly;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.UnitTests;

internal record TestDomainEvent(Guid AggregateId, Guid EventId, DateTime OccurredAt) : IDomainEvent;

internal class TestAggregate() : AggregateRoot(Guid.NewGuid())
{
    public void DoSomething()
    {
        RaiseDomainEvent(new TestDomainEvent(Id, Guid.NewGuid(), DateTime.UtcNow));
    }
}

public class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_AddsDomainEvent()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething();

        // Assert
        aggregate.DomainEvents.ShouldHaveSingleItem();
        var domainEvent = aggregate.DomainEvents.First() as TestDomainEvent;
        domainEvent.ShouldNotBeNull();
        aggregate.Id.ShouldBe(domainEvent.AggregateId);
    }

    [Fact]
    public void ClearDomainEvents_ClearsDomainEventsList()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }
    
    [Fact]
    public void DomainEvents_IsReadOnly()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        // Act & Assert
        aggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}