using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheSupremacy.ProperDomain.Events;
using TheSupremacy.ProperDomain.UnitTests.TestHelpers;

namespace TheSupremacy.ProperDomain.UnitTests;

public class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_AddsDomainEvent()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        aggregate.DoSomething();

        // Assert
        aggregate.DomainEvents.ShouldHaveSingleItem();
        var domainEvent = aggregate.DomainEvents.First() as TestAggregateDomainEvent;
        domainEvent.ShouldNotBeNull();
        aggregate.Id.ShouldBe(domainEvent.AggregateId);
    }

    [Fact]
    public void ClearDomainEvents_ClearsDomainEventsList()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
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
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.DoSomething();

        // Act & Assert
        aggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
    
    [Fact]
    public void ParameterlessConstructor_ForEfCore_CreatesAggregate()
    {
        var aggregate = (TestAggregate)Activator.CreateInstance(typeof(TestAggregate), true)!;
        aggregate.ShouldNotBeNull();
    }
}