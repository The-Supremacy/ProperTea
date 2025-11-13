using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.Identity.Api;
using ProperTea.Identity.Kernel.Data;
using ProperTea.Identity.Kernel.IntegrationEvents;
using TheSupremacy.ProperIntegrationEvents;
using TheSupremacy.ProperIntegrationEvents.Kafka;
using TheSupremacy.ProperIntegrationEvents.Outbox;
using TheSupremacy.ProperIntegrationEvents.Outbox.Ef;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace ProperTea.Identity.IntegrationTests.Api.Setup;

public class IdentityServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("propertea_identity_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private KafkaContainer? _kafkaContainer;

    public string BootstrapServers { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProperTeaIdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithCleanUp(true)
            .Build();

        await _kafkaContainer.StartAsync();
        BootstrapServers = _kafkaContainer.GetBootstrapAddress();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();

        if (_kafkaContainer != null)
        {
            await _kafkaContainer.StopAsync();
            await _kafkaContainer.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType
                                                           == typeof(DbContextOptions<ProperTeaIdentityDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ProperTeaIdentityDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.AddAuthentication().AddScheme<TestExternalSchemeOptions, TestExternalSchemeHandler>(
                TestExternalSchemeHandler.DefaultScheme, options => { });

            services.AddTransient<IAuthenticationSchemeProvider, TestSchemeProvider>();

            services.AddProperIntegrationEvents(e =>
                    e.AddEventType<UserCreatedIntegrationEvent>("UserCreated"))
                .AddKafka(kafka => kafka.BootstrapServers = BootstrapServers)
                .AddOutbox()
                .AddEntityFrameworkStores<ProperTeaIdentityDbContext>();
        });
    }
}