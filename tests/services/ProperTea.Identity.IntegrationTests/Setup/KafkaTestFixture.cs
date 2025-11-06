using Confluent.Kafka;
using Testcontainers.Kafka;

namespace ProperTea.Identity.IntegrationTests.Setup;

public class KafkaTestFixture : IAsyncLifetime
{
    private KafkaContainer? _kafkaContainer;

    public string BootstrapServers { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.9.4")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithCleanUp(true)
            .Build();

        await _kafkaContainer.StartAsync();
        BootstrapServers = _kafkaContainer.GetBootstrapAddress();
    }

    public async Task DisposeAsync()
    {
        if (_kafkaContainer != null)
        {
            await _kafkaContainer.StopAsync();
            await _kafkaContainer.DisposeAsync();
        }
    }

    public IConsumer<string, string> CreateConsumer(string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }
}