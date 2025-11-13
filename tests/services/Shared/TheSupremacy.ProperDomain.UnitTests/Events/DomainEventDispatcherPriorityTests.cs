using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.UnitTests.Events;

public class DomainEventDispatcherPriorityTests
{
    [Fact]
    public async Task DispatchAllAsync_AddOrderedEvent_DispatchedInFifoOrder()
    {
        // Arrange
        var executionOrder = new List<Guid>();
        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();
        var event3Id = Guid.NewGuid();

        var services = new ServiceCollection();
        var handler = new Mock<IDomainEventHandler<TestEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => executionOrder.Add(e.EventId))
            .Returns(Task.CompletedTask);

        services.AddSingleton(handler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider);

        dispatcher.Enqueue(new TestEvent(event1Id, DateTime.UtcNow));
        dispatcher.Enqueue(new TestEvent(event2Id, DateTime.UtcNow));
        dispatcher.Enqueue(new TestEvent(event3Id, DateTime.UtcNow));

        // Act
        await dispatcher.DispatchAllAsync();

        // Assert
        executionOrder.ShouldBe([event1Id, event2Id, event3Id]);
    }
}

public record TestEvent(Guid EventId, DateTime OccurredAt) : DomainEvent(EventId, OccurredAt);