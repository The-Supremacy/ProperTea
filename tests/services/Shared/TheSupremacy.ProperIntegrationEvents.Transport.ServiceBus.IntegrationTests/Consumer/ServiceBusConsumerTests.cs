using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;
using TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.IntegrationTests.TestHelpers;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.IntegrationTests.Consumer;

[Collection("ServiceBus Collection")]
public class ServiceBusConsumerTests(ServiceBusFixture fixture) : IAsyncLifetime
{
    private readonly ServiceBusFixture _fixture = fixture;
    private ServiceProvider? _serviceProvider;
    private ServiceBusIntegrationEventConsumer? _consumer;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        var builder = services.AddProperIntegrationEvents(e =>
        {
            e.AddEventType<TestIntegrationEvent>("TestEvent");
        });

        builder.AddServiceBusConsumer(config =>
        {
            config.ConnectionString = _fixture.ConnectionString;
            config.TopicName = ServiceBusFixture.TopicName;
            config.SubscriptionName = ServiceBusFixture.SubscriptionName;
            config.MaxConcurrentMessages = 1;
        });

        builder.AddEventHandler<TestIntegrationEvent, TestIntegrationEventHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _consumer = _serviceProvider.GetRequiredService<ServiceBusIntegrationEventConsumer>();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Consumer_WithValidMessage_InvokesHandler()
    {
        // Arrange
        await _consumer!.StartAsync();

        var testEvent = new TestIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid())
        {
            TestData = "integration-test-data"
        };

        var payload = JsonSerializer.Serialize(testEvent);

        await using var client = new ServiceBusClient(_fixture.ConnectionString);
        await using var sender = client.CreateSender(ServiceBusFixture.TopicName);

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
        {
            MessageId = testEvent.Id.ToString(),
            CorrelationId = testEvent.CorrelationId?.ToString(),
            Subject = "TestEvent",
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["EventType"] = "TestEvent"
            }
        };

        // Act
        await sender.SendMessageAsync(message);

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert
        var handler = _serviceProvider!.GetRequiredService<IIntegrationEventHandler<TestIntegrationEvent>>()
            as TestIntegrationEventHandler;

        handler.ShouldNotBeNull();
        handler.ProcessedEvents.ShouldContain(e =>
            e.Id == testEvent.Id &&
            e.TestData == "integration-test-data" &&
            e.CorrelationId == testEvent.CorrelationId);
    }

    [Fact]
    public async Task Consumer_WithUnknownEventType_DeadLettersMessage()
    {
        // Arrange
        await _consumer!.StartAsync();

        await using var client = new ServiceBusClient(_fixture.ConnectionString);
        await using var sender = client.CreateSender(ServiceBusFixture.TopicName);

        var message = new ServiceBusMessage("{}")
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = "UnknownEvent",
            ApplicationProperties =
            {
                ["EventType"] = "UnknownEvent"
            }
        };

        // Act
        await sender.SendMessageAsync(message);
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert - Check dead-letter queue
        await using var deadLetterReceiver = client.CreateReceiver(
            ServiceBusFixture.TopicName,
            ServiceBusFixture.SubscriptionName,
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

        var deadLetteredMessage = await deadLetterReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        deadLetteredMessage.ShouldNotBeNull();
        deadLetteredMessage.DeadLetterReason.ShouldBe("UnknownEventType");
        deadLetteredMessage.DeadLetterErrorDescription.ShouldContain("UnknownEvent");
    }

    [Fact]
    public async Task Consumer_WithInvalidJson_DeadLettersMessage()
    {
        // Arrange
        await _consumer!.StartAsync();

        await using var client = new ServiceBusClient(_fixture.ConnectionString);
        await using var sender = client.CreateSender(ServiceBusFixture.TopicName);

        var message = new ServiceBusMessage("invalid json {{{")
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = "TestEvent",
            ApplicationProperties =
            {
                ["EventType"] = "TestEvent"
            }
        };

        // Act
        await sender.SendMessageAsync(message);
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert
        await using var deadLetterReceiver = client.CreateReceiver(
            ServiceBusFixture.TopicName,
            ServiceBusFixture.SubscriptionName,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        var deadLetteredMessage = await deadLetterReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        deadLetteredMessage.ShouldNotBeNull();
        deadLetteredMessage.DeadLetterReason.ShouldBe("DeserializationError");
    }

    [Fact]
    public async Task Consumer_ProcessesMultipleMessagesInOrder()
    {
        // Arrange
        await _consumer!.StartAsync();

        var events = Enumerable.Range(1, 5)
            .Select(i => new TestIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid())
            {
                TestData = $"message-{i}"
            })
            .ToList();

        await using var client = new ServiceBusClient(_fixture.ConnectionString);
        await using var sender = client.CreateSender(ServiceBusFixture.TopicName);

        // Act
        foreach (var evt in events)
        {
            var payload = JsonSerializer.Serialize(evt);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = evt.Id.ToString(),
                Subject = "TestEvent",
                ApplicationProperties =
                {
                    ["EventType"] = "TestEvent"
                }
            };

            await sender.SendMessageAsync(message);
        }

        await Task.Delay(TimeSpan.FromSeconds(5));

        // Assert
        var handler = _serviceProvider!.GetRequiredService<IIntegrationEventHandler<TestIntegrationEvent>>()
            as TestIntegrationEventHandler;

        handler.ShouldNotBeNull();
        handler.ProcessedEvents.Count.ShouldBe(5);

        foreach (var evt in events)
        {
            handler.ProcessedEvents.ShouldContain(e => e.Id == evt.Id);
        }
    }

    public async Task DisposeAsync()
    {
        if (_consumer != null)
        {
            await _consumer.StopAsync();
            await _consumer.DisposeAsync();
        }

        _serviceProvider?.Dispose();
    }
}
