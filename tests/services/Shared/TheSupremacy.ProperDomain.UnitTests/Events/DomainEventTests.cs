// tests/services/Shared/TheSupremacy.ProperDomain.UnitTests/Events/DomainEventTests.cs

using Shouldly;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.UnitTests.Events;

public class DomainEventTests
{
    private record TestEvent(Guid EventId, DateTime OccurredAt, string Data) : DomainEvent(EventId, OccurredAt);

    [Fact]
    public void DomainEvent_SetsEventId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        // Act
        var @event = new TestEvent(eventId, occurredAt, "test");

        // Assert
        eventId.ShouldBe(@event.EventId);
    }

    [Fact]
    public void DomainEvent_SetsOccurredAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        // Act
        var @event = new TestEvent(eventId, occurredAt, "test");

        // Assert
        occurredAt.ShouldBe(@event.OccurredAt);
    }

    [Fact]
    public void DomainEvent_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        // Act
        var event1 = new TestEvent(eventId, occurredAt, "data");
        var event2 = new TestEvent(eventId, occurredAt, "data");

        // Assert
        event1.ShouldBe(event2);
    }
}