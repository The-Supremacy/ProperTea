using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Publisher;

internal class ServiceBusMessageTransport : IMessageTransport, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusPublisherConfiguration _publisherConfiguration;
    private readonly ILogger<ServiceBusMessageTransport> _logger;

    public ServiceBusMessageTransport(
        IOptions<ServiceBusPublisherConfiguration> options,
        ILogger<ServiceBusMessageTransport> logger)
    {
        _publisherConfiguration = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_publisherConfiguration.ConnectionString))
            throw new InvalidOperationException("ServiceBus ConnectionString is required");

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = _publisherConfiguration.MaxRetries,
                Delay = _publisherConfiguration.RetryDelay,
                Mode = ServiceBusRetryMode.Exponential
            }
        };

        _client = new ServiceBusClient(_publisherConfiguration.ConnectionString, clientOptions);
    }

    public async Task SendAsync(
        string topic,
        string eventType,
        string payload,
        Dictionary<string, string> headers,
        CancellationToken ct = default)
    {
        var messageId = headers.GetValueOrDefault("MessageId") ?? Guid.NewGuid().ToString();
        var correlationId = headers.GetValueOrDefault("CorrelationId");
        
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = messageId,
            ["CorrelationId"] = correlationId ?? "null",
            ["EventType"] = eventType,
            ["Topic"] = topic
        });
        
        await using var sender = _client.CreateSender(topic);

        try
        {
            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
                Subject = eventType,
                MessageId = headers.GetValueOrDefault("MessageId") ?? Guid.NewGuid().ToString(),
                CorrelationId = headers.GetValueOrDefault("CorrelationId"),
                TimeToLive = _publisherConfiguration.MessageTimeToLive
            };

            foreach (var (key, value) in headers)
            {
                message.ApplicationProperties[key] = value;
            }

            await sender.SendMessageAsync(message, ct);

            _logger.LogInformation(
                "Sent message to topic {Topic} (EventType: {EventType}, MessageId: {MessageId})",
                topic, eventType, message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to topic {Topic} (EventType: {EventType})",
                topic, eventType);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
