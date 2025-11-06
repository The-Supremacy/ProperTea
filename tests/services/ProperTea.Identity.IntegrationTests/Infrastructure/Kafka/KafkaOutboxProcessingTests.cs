using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.Identity.IntegrationTests.Setup;
using ProperTea.Identity.Kernel.Data;
using ProperTea.Identity.Kernel.IntegrationEvents;
using ProperTea.ProperIntegrationEvents;
using ProperTea.ProperIntegrationEvents.Kafka;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;
using Shouldly;

namespace ProperTea.Identity.IntegrationTests.Infrastructure.Kafka;

[Collection("Kafka")]
public class KafkaOutboxProcessingTests(KafkaTestFixture kafkaFixture)
{
    [Fact]
    public async Task ProcessOutboxMessages_MessageIsPending_PublishesToKafka()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();

        var userId = Guid.NewGuid();
        var integrationEvent =
            new UserCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, userId, DateTime.UtcNow);

        await publisher.PublishAsync(topic, integrationEvent);
        await dbContext.SaveChangesAsync();

        Thread.Sleep(500); // Ensure Kafka is ready to consume

        using var consumer = kafkaFixture.CreateConsumer($"test-{Guid.NewGuid()}");
        consumer.Subscribe(topic);

        // Act
        await processor.ProcessOutboxMessagesAsync(10);

        // Assert - Outbox status
        var processed = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
        processed.ShouldNotBeNull();
        processed!.Status.ShouldBe(OutboxMessageStatus.Published);
        processed.PublishedAt.ShouldNotBeNull();

        // Assert - Kafka message
        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.ShouldNotBeNull();
        result.Message.Key.ShouldBe(integrationEvent.Id.ToString());

        var received = JsonSerializer.Deserialize<UserCreatedIntegrationEvent>(result.Message.Value);
        received.ShouldNotBeNull();
        received!.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task ProcessOutboxMessages_EventTypeUnknown_MarksAsFailed()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e => { }) // No event types registered
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var integrationEvent = new UserCreatedIntegrationEvent(messageId, DateTime.UtcNow, userId, DateTime.UtcNow);

        await publisher.PublishAsync(topic, integrationEvent);
        await dbContext.SaveChangesAsync();

        // Act
        await processor.ProcessOutboxMessagesAsync(10);

        // Assert
        var processed = await messagesService.GetMessageByIdAsync(messageId);
        processed.ShouldNotBeNull();
        processed!.Status.ShouldBe(OutboxMessageStatus.Failed);
        processed.LastError!.ShouldContain("Unknown event type");
    }

    [Fact]
    public async Task ProcessOutboxMessages_MessagesBatch_ProcessesMultipleMessages()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messageIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var integrationEvent =
                new UserCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow);

            messageIds.Add(integrationEvent.Id);
            await publisher.PublishAsync(topic, integrationEvent);
        }

        await dbContext.SaveChangesAsync();
        Thread.Sleep(500); // Ensure Kafka is ready to consume

        using var consumer = kafkaFixture.CreateConsumer($"test-{Guid.NewGuid()}");
        consumer.Subscribe(topic);

        // Act
        await processor.ProcessOutboxMessagesAsync(10);

        // Assert - All published
        foreach (var id in messageIds)
        {
            var processed = await messagesService.GetMessageByIdAsync(id);
            processed.ShouldNotBeNull();
            processed!.Status.ShouldBe(OutboxMessageStatus.Published);
        }

        // Assert - All events in Kafka
        var received = new List<ConsumeResult<string, string>>();
        for (var i = 0; i < 5; i++)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(5));
            result.ShouldNotBeNull();
            received.Add(result);
        }

        received.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ProcessOutboxMessages_PublishingFails_IncrementsRetryCount()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = "invalid-broker:9092")
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var integrationEvent = new UserCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            DateTime.UtcNow);

        await publisher.PublishAsync(topic, integrationEvent);
        await dbContext.SaveChangesAsync();

        // Act - Now works because producer is transient
        await processor.ProcessOutboxMessagesAsync(batchSize: 10);

        // Assert
        var processed = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
        processed!.Status.ShouldBe(OutboxMessageStatus.Pending);
        processed.RetryCount.ShouldBe(1);
        processed.NextRetryAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProcessOutboxMessages_MaxRetriesExceeded_StaysInFailedStatus()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = "invalid-broker:9092")
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var integrationEvent = new UserCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            DateTime.UtcNow);

        await publisher.PublishAsync(topic, integrationEvent);
        await dbContext.SaveChangesAsync();

        // Act - Process multiple times (simulate retries)
        for (var i = 0; i < 10; i++)
            await processor.ProcessOutboxMessagesAsync();

        // Assert - Should stop retrying after max attempts
        var processed = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
        processed.ShouldNotBeNull();
        processed!.Status.ShouldBe(OutboxMessageStatus.Failed);
        processed.RetryCount.ShouldBeLessThanOrEqualTo(5); // Assuming max retries = 5
    }

    [Fact]
    public async Task PublishAsync_SameAggregateId_GoesToSamePartition()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-events-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var userId = Guid.NewGuid();
        var events = new List<UserCreatedIntegrationEvent>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, userId, DateTime.UtcNow),
            new(Guid.NewGuid(), DateTime.UtcNow, userId, DateTime.UtcNow),
            new(Guid.NewGuid(), DateTime.UtcNow, userId, DateTime.UtcNow)
        };

        foreach (var evt in events)
            await publisher.PublishAsync(topic, evt);
        await dbContext.SaveChangesAsync();

        using var consumer = kafkaFixture.CreateConsumer($"test-{Guid.NewGuid()}");
        consumer.Subscribe(topic);

        // Act
        await processor.ProcessOutboxMessagesAsync();

        // Assert - All messages should be in same partition
        var partitions = new HashSet<int>();
        for (var i = 0; i < 3; i++)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(5));
            result.ShouldNotBeNull();
            partitions.Add(result.Partition.Value);
        }

        partitions.Count.ShouldBe(1); // All in same partition
    }

    [Fact]
    public async Task ProcessOutboxMessages_MoreMessagesThanBatchSize_ProcessesInBatches()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        // Create 15 messages
        var messageIds = new List<Guid>();
        for (var i = 0; i < 15; i++)
        {
            var evt = new UserCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                DateTime.UtcNow);
            messageIds.Add(evt.Id);
            await publisher.PublishAsync(topic, evt);
        }

        await dbContext.SaveChangesAsync();

        // Act - Process with batch size of 5
        await processor.ProcessOutboxMessagesAsync(5);

        // Assert - Only first 5 should be processed
        var processed = 0;
        var pending = 0;

        foreach (var id in messageIds)
        {
            var msg = await messagesService.GetMessageByIdAsync(id);
            if (msg!.Status == OutboxMessageStatus.Published)
                processed++;
            else if (msg.Status == OutboxMessageStatus.Pending)
                pending++;
        }

        processed.ShouldBe(5);
        pending.ShouldBe(10);

        // Process remaining
        await processor.ProcessOutboxMessagesAsync();

        // Assert - All should now be processed
        foreach (var id in messageIds)
        {
            var msg = await messagesService.GetMessageByIdAsync(id);
            msg!.Status.ShouldBe(OutboxMessageStatus.Published);
        }
    }
    
    [Fact]
    public async Task PublishAsync_IncludesAllMetadataHeaders()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var topic = $"user-created-{Guid.NewGuid()}";
    
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddProperIntegrationEvents(e =>
                e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
            .AddKafka(kafka => kafka.BootstrapServers = kafkaFixture.BootstrapServers)
            .AddOutbox()
            .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();
    
        var serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();
    
        var integrationEvent = new UserCreatedIntegrationEvent(
            Guid.NewGuid(), 
            DateTime.UtcNow, 
            Guid.NewGuid(), 
            DateTime.UtcNow);
    
        await publisher.PublishAsync(topic, integrationEvent);
        await dbContext.SaveChangesAsync();
    
        using var consumer = kafkaFixture.CreateConsumer($"test-{Guid.NewGuid()}");
        consumer.Subscribe(topic);
    
        // Act
        await processor.ProcessOutboxMessagesAsync(batchSize: 10);
    
        // Assert
        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.ShouldNotBeNull();
    
        var headers = result.Message.Headers;
        headers.ShouldContain(h => h.Key == "event-type");
        headers.ShouldContain(h => h.Key == "event-id");
        headers.ShouldContain(h => h.Key == "correlation-id");
        headers.ShouldContain(h => h.Key == "occurred-at");
    
        var eventTypeHeader = headers.First(h => h.Key == "event-type");
        var eventType = System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
        eventType.ShouldBe(UserCreatedIntegrationEvent.EventTypeName);
    }
    
    [Fact]
public async Task ProcessOutboxMessages_TransientFailure_RetriesWithBackoff()
{
    // Arrange
    var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    var topic = $"user-created-{Guid.NewGuid()}";

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(dbContext);
    services.AddSingleton(new OutboxConfiguration
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = TimeSpan.FromSeconds(1),
        RetryDelayMultiplier = 2.0
    });
    services.AddProperIntegrationEvents(e =>
            e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
        .AddKafka(kafka => kafka.BootstrapServers = "invalid-broker:9092") // Simulates transient failure
        .AddOutbox()
        .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

    var serviceProvider = services.BuildServiceProvider();
    var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();
    var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
    var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

    var integrationEvent = new UserCreatedIntegrationEvent(
        Guid.NewGuid(),
        DateTime.UtcNow,
        Guid.NewGuid(),
        DateTime.UtcNow);

    await publisher.PublishAsync(topic, integrationEvent);
    await dbContext.SaveChangesAsync();

    // Act - First attempt fails
    await processor.ProcessOutboxMessagesAsync(batchSize: 10);

    // Assert - Still pending, retry scheduled
    var message = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
    message.ShouldNotBeNull();
    message!.Status.ShouldBe(OutboxMessageStatus.Pending);
    message.RetryCount.ShouldBe(1);
    message.NextRetryAt.ShouldNotBeNull();
    message.NextRetryAt!.Value.ShouldBeGreaterThan(DateTime.UtcNow); // Future time

    // Act - Try again before retry time
    await processor.ProcessOutboxMessagesAsync(batchSize: 10);

    // Assert - Still pending, retry count unchanged (not ready yet)
    message = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
    message!.RetryCount.ShouldBe(1); // Same as before

    // Act - Wait for retry delay and try again
    await Task.Delay(TimeSpan.FromSeconds(1.5));
    await processor.ProcessOutboxMessagesAsync(batchSize: 10);

    // Assert - Retry attempted
    message = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
    message!.RetryCount.ShouldBe(2);
}

[Fact]
public async Task ProcessOutboxMessages_MaxRetriesExceeded_MarksAsPermanentlyFailed()
{
    // Arrange
    var dbContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
    var topic = $"user-created-{Guid.NewGuid()}";

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(dbContext);
    services.AddSingleton(new OutboxConfiguration
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = TimeSpan.FromMilliseconds(10)
    });
    services.AddProperIntegrationEvents(e =>
            e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName))
        .AddKafka(kafka => kafka.BootstrapServers = "invalid-broker:9092")
        .AddOutbox()
        .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

    var serviceProvider = services.BuildServiceProvider();
    var publisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();
    var messagesService = serviceProvider.GetRequiredService<IOutboxMessagesService>();

    var integrationEvent = new UserCreatedIntegrationEvent(
        Guid.NewGuid(),
        DateTime.UtcNow,
        Guid.NewGuid(),
        DateTime.UtcNow);

    await publisher.PublishAsync(topic, integrationEvent);
    await dbContext.SaveChangesAsync();

    // Act - Retry 4 times (initial + 3 retries)
    for (int i = 0; i < 4; i++)
    {
        // Rebuild service provider to get fresh producer instance
        serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();
        
        await processor.ProcessOutboxMessagesAsync(batchSize: 10);
        await Task.Delay(20);
        
        // Dispose to force new producer next iteration
        if (serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    // Assert
    var message = await messagesService.GetMessageByIdAsync(integrationEvent.Id);
    message.ShouldNotBeNull();
    message!.Status.ShouldBe(OutboxMessageStatus.Failed);
    message.RetryCount.ShouldBe(3);
    message.NextRetryAt.ShouldBeNull();
    message.LastError!.ShouldContain("Max retries exceeded");
}
}