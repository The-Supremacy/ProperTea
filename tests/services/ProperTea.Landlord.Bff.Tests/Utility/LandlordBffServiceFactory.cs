using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Testcontainers.Redis;
using WireMock.Server;

namespace ProperTea.Landlord.Bff.Tests.Utility;

public class LandlordBffServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7")
        .Build();

    public WireMockServer IdentityServiceMock { get; } = WireMockServer.Start();

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        IdentityServiceMock.Stop();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to use our test dependencies
        builder.ConfigureAppConfiguration(config =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "ConnectionStrings:Redis", _redisContainer.GetConnectionString() },
                { "ReverseProxy:Clusters:identity-cluster:Destinations:destination1:Address", IdentityServiceMock.Url },
                { "ServiceEndpoints:Identity", IdentityServiceMock.Url }
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Optional: If you need to replace or mock other services, you can do it here.
        });
    }
}