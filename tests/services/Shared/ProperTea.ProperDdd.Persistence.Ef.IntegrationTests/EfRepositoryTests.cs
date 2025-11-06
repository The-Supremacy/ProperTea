using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperDdd.Persistence.Ef.IntegrationTests.Setup;

namespace ProperTea.ProperDdd.Persistence.Ef.IntegrationTests;

[Collection("DatabaseCollection")]
public class EfRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public EfRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

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
        Assert.NotNull(result);
        Assert.Equal("Test Add", result.Name);
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
        Assert.NotNull(result);
        Assert.Equal(aggregate.Id, result.Id);
        Assert.Equal("Test Get", result.Name);
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
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
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
        Assert.Null(result);
    }

    private async Task<(EfRepository<TestAggregate> repository, TestDbContext dbContext)> GetRepositoryAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));

        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var repository = new EfRepository<TestAggregate>(dbContext);
        return (repository, dbContext);
    }
}