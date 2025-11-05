using Testcontainers.PostgreSql;

namespace ProperTea.ProperSagas.Ef.Tests.Setup;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("sagastestdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _postgreSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

