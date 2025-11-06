using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProperTea.ProperIntegrationEvents.ServiceBus;

public class ServiceBusIntegrationEventPublisher : IIntegrationEventPublisher,
    IExternalIntegrationEventPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<ServiceBusIntegrationEventPublisher> _logger;

    public ServiceBusIntegrationEventPublisher(
        IOptions<ServiceBusConfiguration> options,
        ILogger<ServiceBusIntegrationEventPublisher> logger)
    {
        _configuration = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_configuration.ConnectionString))
            throw new InvalidOperationException("ServiceBus ConnectionString is required");

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = _configuration.MaxRetries,
                Delay = _configuration.RetryDelay,
                Mode = ServiceBusRetryMode.Exponential
            }
        };

        _client = new ServiceBusClient(_configuration.ConnectionString, clientOptions);

        _logger.LogInformation("ServiceBus client initialized with {MaxRetries} max retries",
            _configuration.MaxRetries);
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        await using var sender = _client.CreateSender(topic);

        try
        {
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
                Subject = integrationEvent.EventType,
                MessageId = integrationEvent.Id.ToString(),
                CorrelationId = integrationEvent.Id.ToString(),
                TimeToLive = _configuration.MessageTimeToLive
            };

            message.ApplicationProperties["EventType"] = integrationEvent.EventType;
            message.ApplicationProperties["AggregateId"] = integrationEvent.Id.ToString();
            message.ApplicationProperties["OccurredAt"] = integrationEvent.OccurredAt;

            await sender.SendMessageAsync(message, ct);

            _logger.LogInformation(
                "Published event {EventType} to topic {Topic} (MessageId: {MessageId})",
                integrationEvent.EventType, topic, message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} to topic {Topic}",
                integrationEvent.EventType, topic);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}