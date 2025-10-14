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
    
    private readonly WireMockServer _identityServiceMock = WireMockServer.Start();
    
    public WireMockServer IdentityServiceMock => _identityServiceMock;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to use our test dependencies
        builder.ConfigureAppConfiguration(config =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "ConnectionStrings:Redis", _redisContainer.GetConnectionString() },
                { "ReverseProxy:Clusters:identity-cluster:Destinations:destination1:Address", _identityServiceMock.Url },
                { "ServiceEndpoints:Identity", _identityServiceMock.Url }
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Optional: If you need to replace or mock other services, you can do it here.
        });
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        _identityServiceMock.Stop();
    }
}