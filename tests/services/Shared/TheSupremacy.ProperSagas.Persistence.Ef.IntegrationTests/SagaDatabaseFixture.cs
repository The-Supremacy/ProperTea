using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace TheSupremacy.ProperSagas.Persistence.Ef.IntegrationTests;

public class SagaDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("saga_test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public SagaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new SagaDbContext(options);
    }
}

public class SagaDbContext : DbContext, ISagaDbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options)
        : base(options)
    {
    }

    public SagaDbContext()
    {
    }

    public DbSet<SagaEntity> Sagas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new SagaEntityConfiguration().Configure(modelBuilder.Entity<SagaEntity>());

        base.OnModelCreating(modelBuilder);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SagaDatabaseFixture>
{
}