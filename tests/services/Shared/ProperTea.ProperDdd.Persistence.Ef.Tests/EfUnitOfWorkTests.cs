using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProperTea.ProperDdd.Events;
using ProperTea.ProperDdd.Persistence.Ef.Tests.Setup;

namespace ProperTea.ProperDdd.Persistence.Ef.Tests;

[Collection("DatabaseCollection")]
public class EfUnitOfWorkTests
{
    private readonly DatabaseFixture _fixture;

    public EfUnitOfWorkTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_SavesChangesAndDispatchEvents()
    {
        // Arrange
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(domainEventDispatcherMock.Object);
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TestDbContext>>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var dbContext = scopedProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var unitOfWork = scopedProvider.GetRequiredService<IUnitOfWork>();
        var aggregate = new TestAggregate("Test Name");
        aggregate.DoSomething();

        await dbContext.Set<TestAggregate>().AddAsync(aggregate);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);
        
        var savedAggregate = await dbContext.TestAggregates.FindAsync(aggregate.Id);
        Assert.NotNull(savedAggregate);
        Assert.Equal("Test Name", savedAggregate.Name);
        
        domainEventDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}