using Azure.Messaging.ServiceBus.Administration;
using DotNet.Testcontainers;
using Testcontainers.ServiceBus;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.IntegrationTests;

public class ServiceBusFixture : IAsyncLifetime
{
    private ServiceBusContainer? _container;

    public string ConnectionString { get; private set; } = null!;
    public const string TopicName = "test-topic";
    public const string SubscriptionName = "test-subscription";

    public async Task InitializeAsync()
    {
        _container = new ServiceBusBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithLogger(ConsoleLogger.Instance)
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
        
        var adminClient = new ServiceBusAdministrationClient(ConnectionString);

        await adminClient.CreateTopicAsync(TopicName);
        await adminClient.CreateSubscriptionAsync(TopicName, SubscriptionName);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        { 
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition("ServiceBus Collection")]
public class ServiceBusCollection : ICollectionFixture<ServiceBusFixture>
{
}