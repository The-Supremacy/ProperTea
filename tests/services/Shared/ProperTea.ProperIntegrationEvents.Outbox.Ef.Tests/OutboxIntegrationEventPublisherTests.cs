using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;
using Shouldly;
using System.Text.Json;
using ProperTea.ProperIntegrationEvents.Outbox.Ef.Tests.Setup;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef.Tests;

public record TestIntegrationEvent(Guid Id, DateTime OccurredAt) : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "test-event";
}

[Collection("DatabaseCollection")]
public class OutboxIntegrationEventPublisherTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public OutboxIntegrationEventPublisherTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private IServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));
        
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TestDbContext>());

        services.AddTransient<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        
        services.AddProperIntegrationEvents()
            .UseOutbox(o => o.UseEntityFrameworkStorage<TestDbContext>());
        
        _scope = services.BuildServiceProvider().CreateScope();
        _serviceProvider = _scope.ServiceProvider;

        var dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
    }

    [Fact]
    public async Task PublishAsync_SavesEventToOutbox()
    {
        // Arrange
        var dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var publisher = _scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var testEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow);

        // Act
        await publisher.PublishAsync("test_topic", testEvent);
        await dbContext.SaveChangesAsync(); // Simulate the unit of work completing

        // Assert
        var outboxMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync();
        outboxMessage.ShouldNotBeNull();
        outboxMessage.Topic.ShouldBe("test_topic");
        outboxMessage.EventType.ShouldBe(testEvent.EventType);
        outboxMessage.Payload.ShouldBe(JsonSerializer.Serialize(testEvent, testEvent.GetType()));
        outboxMessage.PublishedAt.ShouldBeNull();
    }
}
