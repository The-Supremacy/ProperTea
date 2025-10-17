using System.Linq;
using ProperTea.ProperDdd.Events;
using Xunit;

namespace ProperTea.ProperDdd.Tests;

internal record TestDomainEvent(Guid AggregateId, Guid EventId, DateTime OccurredAt) : IDomainEvent;

internal class TestAggregate : AggregateRoot
{
    public TestAggregate() : base(Guid.NewGuid())
    {
    }

    public void DoSomething()
    {
        RaiseDomainEvent(new TestDomainEvent(Id, Guid.NewGuid(), DateTime.UtcNow));
    }
}

public class AggregateRootTests
{
    [Fact]
    public void WhenActionIsPerformed_ThenDomainEventIsAdded()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething();

        // Assert
        Assert.Single(aggregate.DomainEvents);
        var domainEvent = aggregate.DomainEvents.First() as TestDomainEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(aggregate.Id, domainEvent.AggregateId);
    }

    [Fact]
    public void WhenDomainEventsAreCleared_ThenDomainEventListIsEmpty()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }
}