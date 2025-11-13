using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TheSupremacy.ProperDomain.Events;
using TheSupremacy.ProperDomain.UnitTests.TestHelpers;

namespace TheSupremacy.ProperDomain.UnitTests.Events;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAllAsync_AddEvent_DispatchedInFifoOrder()
    {
        // Arrange
        var executionOrder = new List<Guid>();
        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();
        var event3Id = Guid.NewGuid();

        var services = new ServiceCollection();
        var handler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestDomainEvent, CancellationToken>((e, _) => executionOrder.Add(e.EventId))
            .Returns(Task.CompletedTask);

        services.AddSingleton(handler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider);

        dispatcher.Enqueue(new TestDomainEvent(event1Id, DateTime.UtcNow));
        dispatcher.Enqueue(new TestDomainEvent(event2Id, DateTime.UtcNow));
        dispatcher.Enqueue(new TestDomainEvent(event3Id, DateTime.UtcNow));

        // Act
        await dispatcher.DispatchAllAsync();

        // Assert
        executionOrder.ShouldBe([event1Id, event2Id, event3Id]);
    }
}

