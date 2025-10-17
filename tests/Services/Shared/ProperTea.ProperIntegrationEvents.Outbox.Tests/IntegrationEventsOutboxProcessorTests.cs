using Microsoft.Extensions.Logging;
using Moq;

namespace ProperTea.ProperIntegrationEvents.Outbox.Tests;

public record TestEvent(Guid Id, DateTime OccurredAt) : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "some-event-type";
}

public class IntegrationEventsOutboxProcessorTests
{
    private readonly Mock<IOutboxMessagesService> _outboxMessagesServiceMock;
    private readonly Mock<IExternalIntegrationEventPublisher> _integrationEventPublisherMock;
    private readonly Mock<IIntegrationEventTypeResolver> _typeResolver;
    private readonly Mock<ILogger<IntegrationEventsOutboxProcessor>> _loggerMock;
    private readonly IntegrationEventsOutboxProcessor _outboxProcessor;

    public IntegrationEventsOutboxProcessorTests()
    {
        _outboxMessagesServiceMock = new Mock<IOutboxMessagesService>();
        _integrationEventPublisherMock = new Mock<IExternalIntegrationEventPublisher>();
        _typeResolver = new Mock<IIntegrationEventTypeResolver>();
        _loggerMock = new Mock<ILogger<IntegrationEventsOutboxProcessor>>();
        _outboxProcessor = new IntegrationEventsOutboxProcessor(
            _outboxMessagesServiceMock.Object,
            _integrationEventPublisherMock.Object,
            _typeResolver.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_NoMessagesToProcess_ShouldDoNothing()
    {
        // Arrange
        _outboxMessagesServiceMock.Setup(s => s.GetPendingOutboxMessagesAsync(
                It.IsAny<int>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _outboxProcessor.ProcessOutboxMessagesAsync(10, CancellationToken.None);

        // Assert
        _integrationEventPublisherMock.Verify(
            p => p.PublishAsync(
            It.IsAny<string>(), It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WithMessagesToProcess_ShouldPublishAndMarkAsProcessed()
    {
        // Arrange
        var eventType = "some-event-type";
        var topic = "some-topic";
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            EventType = eventType,
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        _outboxMessagesServiceMock.Setup(s => s.GetPendingOutboxMessagesAsync(
                It.IsAny<int>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        _typeResolver.Setup(s => s.ResolveType(eventType)).Returns(typeof(TestEvent));
        
        // Act
        await _outboxProcessor.ProcessOutboxMessagesAsync(10, CancellationToken.None);

        // Assert
        _integrationEventPublisherMock.Verify(p => 
                p.PublishAsync(It.IsAny<string>(), It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _outboxMessagesServiceMock.Verify(s => 
            s.SaveMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenPublishFails_ShouldMarkMessageAsFailed()
    {
        // Arrange
        var eventType = "some-event-type";
        var topic = "some-topic";
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            EventType = eventType,
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        _outboxMessagesServiceMock.Setup(s => s.GetPendingOutboxMessagesAsync(
                It.IsAny<int>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        _typeResolver.Setup(s => s.ResolveType(eventType)).Returns(typeof(TestEvent));
        _integrationEventPublisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("test exception"));

        // Act
        await _outboxProcessor.ProcessOutboxMessagesAsync(10, CancellationToken.None);

        // Assert
        _outboxMessagesServiceMock.Verify(s => s.SaveMessageAsync(
            It.Is<OutboxMessage>(m => m.Status == OutboxMessageStatus.Failed), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenTypeResolutionFails_ShouldMarkMessageAsFailed()
    {
        // Arrange
        var eventType = "unknown-event-type";
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "some-topic",
            EventType = eventType,
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        _outboxMessagesServiceMock.Setup(s => s.GetPendingOutboxMessagesAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        _typeResolver.Setup(s => s.ResolveType(eventType)).Returns((Type)null);

        // Act
        await _outboxProcessor.ProcessOutboxMessagesAsync(10, CancellationToken.None);

        // Assert
        _integrationEventPublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _outboxMessagesServiceMock.Verify(s => s.SaveMessageAsync(
            It.Is<OutboxMessage>(m => m.Status == OutboxMessageStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenPayloadDeserializationFails_ShouldMarkMessageAsFailed()
    {
        // Arrange
        var eventType = "some-event-type";
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "some-topic",
            EventType = eventType,
            Payload = "invalid-json",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        _outboxMessagesServiceMock.Setup(s => s.GetPendingOutboxMessagesAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        _typeResolver.Setup(s => s.ResolveType(eventType)).Returns(typeof(TestEvent));

        // Act
        await _outboxProcessor.ProcessOutboxMessagesAsync(10, CancellationToken.None);

        // Assert
        _integrationEventPublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _outboxMessagesServiceMock.Verify(s => s.SaveMessageAsync(
            It.Is<OutboxMessage>(m => m.Status == OutboxMessageStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
