using System.Globalization;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProperTea.ProperIntegrationEvents.Kafka;

public class KafkaIntegrationEventPublisher : IExternalIntegrationEventPublisher, IAsyncDisposable
{
    private readonly ILogger<KafkaIntegrationEventPublisher> _logger;
    private readonly IProducer<string, string> _producer;

    public KafkaIntegrationEventPublisher(
        IOptions<KafkaConfiguration> options,
        ILogger<KafkaIntegrationEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = options.Value.ClientId,
            Acks = MapAcks(options.Value.Acks),
            MessageTimeoutMs = options.Value.MessageTimeoutMs,
            RequestTimeoutMs = options.Value.RequestTimeoutMs,
            CompressionType = MapCompressionType(options.Value.CompressionType),
            EnableIdempotence = true,
            MaxInFlight = 5,
            LingerMs = 20
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka error: {Reason} ({Code})", error.Reason, error.Code);
            })
            .Build();

        _logger.LogInformation("Kafka producer initialized with servers: {Servers}",
            options.Value.BootstrapServers);
    }

    public async ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        await Task.CompletedTask;
    }


    public async Task PublishAsync<TEvent>(string topic, TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        try
        {
            var key = integrationEvent.Id.ToString();
            var value = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(integrationEvent.EventType) },
                    { "occurred-at", Encoding.UTF8.GetBytes(integrationEvent.OccurredAt.ToString(CultureInfo.InvariantCulture)) },
                    { "event-id", Encoding.UTF8.GetBytes(integrationEvent.Id.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(integrationEvent.Id.ToString()) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, ct);

            _logger.LogInformation(
                "Published event {EventType} to topic {Topic} (partition {Partition}, offset {Offset})",
                integrationEvent.EventType, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} to topic {Topic}. Error: {ErrorCode} - {ErrorReason}",
                integrationEvent.EventType, topic, ex.Error.Code, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event {EventType} to topic {Topic}",
                integrationEvent.GetType().Name, topic);
            throw;
        }
    }

    private static Confluent.Kafka.Acks MapAcks(Acks acks)
    {
        return acks switch
        {
            Acks.None => Confluent.Kafka.Acks.None,
            Acks.Leader => Confluent.Kafka.Acks.Leader,
            Acks.All => Confluent.Kafka.Acks.All,
            _ => Confluent.Kafka.Acks.All
        };
    }

    private static Confluent.Kafka.CompressionType MapCompressionType(CompressionType type)
    {
        return type switch
        {
            CompressionType.None => Confluent.Kafka.CompressionType.None,
            CompressionType.Gzip => Confluent.Kafka.CompressionType.Gzip,
            CompressionType.Snappy => Confluent.Kafka.CompressionType.Snappy,
            CompressionType.Lz4 => Confluent.Kafka.CompressionType.Lz4,
            CompressionType.Zstd => Confluent.Kafka.CompressionType.Zstd,
            _ => Confluent.Kafka.CompressionType.Snappy
        };
    }
}