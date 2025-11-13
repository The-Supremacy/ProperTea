using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TheSupremacy.ProperDomain.Events;
using TheSupremacy.ProperDomain.Exceptions;
using TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests.Setup;

namespace TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests;

[Collection("DatabaseCollection")]
public class EfDomainDomainUnitOfWorkTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_SuccessfulProcessing_SavesChangesAndDispatchEvents()
    {
        // Arrange
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(domainEventDispatcherMock.Object);
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));
        services.AddScoped<IDomainUnitOfWork, EfDomainDomainUnitOfWork<TestDbContext>>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var dbContext = scopedProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var unitOfWork = scopedProvider.GetRequiredService<IDomainUnitOfWork>();
        var aggregate = new TestAggregate("Test Name");
        aggregate.DoSomething();

        // The first dispatch will trigger another one. We limit it to only one extra dispatch.
        var counter = 0;
        domainEventDispatcherMock.Setup(dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (counter < 1)
                    aggregate.DoSomething("Changed Name 1");
                counter++;
            });

        await dbContext.Set<TestAggregate>().AddAsync(aggregate);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.ShouldBeGreaterThan(0);

        var savedAggregate = await dbContext.TestAggregates.FindAsync(aggregate.Id);
        savedAggregate.ShouldNotBeNull();
        "Changed Name 1".ShouldBe(savedAggregate.Name);

        domainEventDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        dbContext.TestAggregates.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task SaveChangesAsync_EndlessRecursiveProcessing_SavesChangesAndDispatchLimitedEvents()
    {
        // Arrange
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(domainEventDispatcherMock.Object);
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));
        services.AddScoped<IDomainUnitOfWork, EfDomainDomainUnitOfWork<TestDbContext>>();

        var limit = 30;
        services.AddScoped<IOptions<ProperDomainOptions>>(sp => Options.Create(new ProperDomainOptions() { MaxEventDispatchIterations = limit }));
        
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var dbContext = scopedProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var unitOfWork = scopedProvider.GetRequiredService<IDomainUnitOfWork>();
        var aggregate = new TestAggregate("Test Name");
        aggregate.DoSomething();

        // Every dispatch will trigger another one.
        domainEventDispatcherMock.Setup(dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()))
            .Callback(() => 
                aggregate.DoSomething("Changed Name 1"));

        await dbContext.Set<TestAggregate>().AddAsync(aggregate);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.ShouldBeGreaterThan(0);

        var savedAggregate = await dbContext.TestAggregates.FindAsync(aggregate.Id);
        savedAggregate.ShouldNotBeNull();
        "Changed Name 1".ShouldBe(savedAggregate.Name);

        domainEventDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(limit));
        dbContext.TestAggregates.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task SaveChangesAsync_ErrorsDuringProcessing_RollbacksChanges()
    {
        // Arrange
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        domainEventDispatcherMock.Setup(dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()))
            .Throws(() => new DomainException("Test Error"));
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(domainEventDispatcherMock.Object);
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));
        services.AddScoped<IDomainUnitOfWork, EfDomainDomainUnitOfWork<TestDbContext>>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var dbContext = scopedProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var unitOfWork = scopedProvider.GetRequiredService<IDomainUnitOfWork>();
        var aggregate = new TestAggregate("Test Name");
        aggregate.DoSomething();

        await dbContext.Set<TestAggregate>().AddAsync(aggregate);

        // Act
        await Should.ThrowAsync<DomainException>(() => unitOfWork.SaveChangesAsync());
        domainEventDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        dbContext.TestAggregates.ShouldBeEmpty();
    }
}