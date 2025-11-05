using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperIntegrationEvents.Outbox.Ef.Tests.Setup;
using Shouldly;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef.Tests;

[Collection("DatabaseCollection")]
public class DbContextOutboxMessagesServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public DbContextOutboxMessagesServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private IServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));

        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TestDbContext>());
        services.AddScoped<IOutboxMessagesService, DbContextOutboxMessagesService>();

        _scope = services.BuildServiceProvider().CreateScope();
        _serviceProvider = _scope.ServiceProvider;

        var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPendingOutboxMessagesAsync_ReturnsOnlyUnpublishedMessages()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        var service = _serviceProvider.GetRequiredService<IOutboxMessagesService>();

        var publishedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "test",
            EventType = "test.event",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Published
        };
        var pendingMessage1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "test",
            EventType = "test.event",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        var pendingMessage2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "test",
            EventType = "test.event",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending
        };
        var pendingMessage4 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "test",
            EventType = "test.event",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Failed
        };

        await dbContext.OutboxMessages.AddRangeAsync(publishedMessage, pendingMessage1, pendingMessage2, pendingMessage4);
        await dbContext.SaveChangesAsync();

        // Act
        var pendingMessages = (await service.GetPendingOutboxMessagesAsync()).ToList();

        // Assert
        pendingMessages.ShouldNotBeNull();
        pendingMessages.Count().ShouldBe(2);
        pendingMessages.ShouldContain(m => m.Id == pendingMessage1.Id);
        pendingMessages.ShouldContain(m => m.Id == pendingMessage2.Id);
        pendingMessages.ShouldNotContain(m => m.Id == publishedMessage.Id);
    }
    
    [Fact]
    public async Task SaveMessageAsync_UpdatesMessageStatus()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        var service = _serviceProvider.GetRequiredService<IOutboxMessagesService>();

        var messageId = Guid.NewGuid();
        var originalMessage = new OutboxMessage
        {
            Id = messageId,
            Topic = "test",
            EventType = "test.event",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending // Start as Pending
        };

        await dbContext.OutboxMessages.AddAsync(originalMessage);
        await dbContext.SaveChangesAsync();

        // Act
        var messageToUpdate = await dbContext.OutboxMessages.FindAsync(messageId);
        messageToUpdate.ShouldNotBeNull();
        
        messageToUpdate.Status = OutboxMessageStatus.Published;
        messageToUpdate.PublishedAt = DateTime.UtcNow;
        
        await service.SaveMessageAsync(messageToUpdate, CancellationToken.None);

        // Assert
        var updatedMessage = await dbContext.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId);
        updatedMessage.ShouldNotBeNull();
        updatedMessage.Status.ShouldBe(OutboxMessageStatus.Published);
        updatedMessage.PublishedAt.ShouldNotBeNull();
    }
}