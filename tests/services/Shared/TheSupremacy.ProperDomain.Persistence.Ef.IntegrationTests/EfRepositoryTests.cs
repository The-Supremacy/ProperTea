using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheSupremacy.ProperDomain.Persistence.Ef;
using TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests.Setup;

namespace TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests;

[Collection("DatabaseCollection")]
public class EfRepositoryTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task AddAsync_AddsAggregateToDatabase()
    {
        // Arrange
        var (repository, dbContext) = await GetRepositoryAsync();
        var aggregate = new TestAggregate("Test Add");

        // Act
        await repository.AddAsync(aggregate);
        await dbContext.SaveChangesAsync();

        // Assert
        var result = await dbContext.TestAggregates.FindAsync(aggregate.Id);
        result.ShouldNotBeNull();
        "Test Add".ShouldBe(result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectAggregate()
    {
        // Arrange
        var (repository, context) = await GetRepositoryAsync();
        var aggregate = new TestAggregate("Test Get");
        await repository.AddAsync(aggregate);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(aggregate.Id);

        // Assert
        result.ShouldNotBeNull();
        aggregate.Id.ShouldBe(result.Id);
        "Test Get".ShouldBe(result.Name);
    }

    [Fact]
    public async Task Update_ModifiesAggregateInDatabase()
    {
        // Arrange
        var (repository, context) = await GetRepositoryAsync();
        var aggregate = new TestAggregate("Initial Name");
        await repository.AddAsync(aggregate);
        await context.SaveChangesAsync();

        // Act
        aggregate.ChangeName("Updated Name");
        repository.Update(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var result = await context.TestAggregates.FindAsync(aggregate.Id);
        result.ShouldNotBeNull();
        "Updated Name".ShouldBe(result.Name);
    }

    [Fact]
    public async Task Delete_RemovesAggregateFromDatabase()
    {
        // Arrange
        var (repository, context) = await GetRepositoryAsync();
        var aggregate = new TestAggregate("To Be Deleted");
        await repository.AddAsync(aggregate);
        await context.SaveChangesAsync();

        // Act
        repository.Delete(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var result = await context.TestAggregates.FindAsync(aggregate.Id);
        result.ShouldBeNull();
    }

    private async Task<(EfRepository<TestAggregate> repository, TestDbContext dbContext)> GetRepositoryAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));

        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var repository = new EfRepository<TestAggregate>(dbContext);
        return (repository, dbContext);
    }
}