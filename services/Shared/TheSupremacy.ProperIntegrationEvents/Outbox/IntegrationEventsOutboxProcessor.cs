using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheSupremacy.ProperIntegrationEvents.Outbox;

public interface IIntegrationEventsOutboxProcessor
{
    Task ProcessOutboxMessagesAsync(int batchSize = 10, CancellationToken cancellationToken = default);
}

public class IntegrationEventsOutboxProcessor(
    IOutboxMessagesRepository messagesService,
    IMessageTransport messageTransport,
    IIntegrationEventTypeResolver typeResolver,
    OutboxConfiguration configuration,
    ILogger<IntegrationEventsOutboxProcessor> logger)
    : IIntegrationEventsOutboxProcessor
{
    public async Task ProcessOutboxMessagesAsync(int batchSize = 10, CancellationToken ct = default)
    {
        var messages = (await messagesService.GetPendingMessagesAsync(batchSize, ct)).ToList();

        foreach (var message in messages)
        {
            if (!IsReadyForRetry(message))
            {
                logger.LogDebug(
                    "Message {MessageId} not ready for retry (NextRetryAt: {NextRetryAt})",
                    message.Id, message.NextRetryAt);
                continue;
            }

            try
            {
                var eventType = typeResolver.ResolveType(message.EventType);
                if (eventType is null)
                {
                    logger.LogWarning(
                        "Unknown event type '{EventType}' for outbox message {MessageId}. Marking as failed.",
                        message.EventType, message.Id);
                    MarkAsFailedPermanently(message, $"Unknown event type: {message.EventType}");
                }
                else
                {
                    var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType) as IntegrationEvent;

                    if (integrationEvent is null)
                    {
                        logger.LogWarning(
                            "Could not deserialize payload for outbox message {MessageId}. Marking as failed.",
                            message.Id);
                        MarkAsFailedPermanently(message, "Payload deserialization returned null.");
                    }
                    else
                    {
                        await PublishMessageAsync(message, ct);
                        message.Status = OutboxMessageStatus.Published;
                        message.PublishedAt = DateTime.UtcNow;
                        message.NextRetryAt = null;

                        logger.LogInformation(
                            "Successfully published message {MessageId} (attempt {Attempt})",
                            message.Id, message.RetryCount + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);

                message.RetryCount++;
                message.LastError = ex.Message;

                if (message.RetryCount >= configuration.MaxRetryAttempts)
                {
                    logger.LogWarning(
                        "Message {MessageId} exceeded max retries ({MaxRetries}). Marking as permanently failed.",
                        message.Id, configuration.MaxRetryAttempts);
                    MarkAsFailedPermanently(message, $"Max retries exceeded: {ex.Message}");
                }
                else
                {
                    var delay = CalculateRetryDelay(message.RetryCount);
                    message.NextRetryAt = DateTime.UtcNow.Add(delay);
                    message.Status = OutboxMessageStatus.Pending;

                    logger.LogInformation(
                        "Message {MessageId} will retry in {Delay} (attempt {Attempt}/{MaxAttempts})",
                        message.Id, delay, message.RetryCount, configuration.MaxRetryAttempts);
                }
            }

            await messagesService.SaveAsync(message, ct);
        }
    }

    private bool IsReadyForRetry(OutboxMessage message)
    {
        return message.NextRetryAt == null || message.NextRetryAt <= DateTime.UtcNow;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delay = configuration.InitialRetryDelay * Math.Pow(configuration.RetryDelayMultiplier, retryCount - 1);
        var delayTimeSpan = TimeSpan.FromSeconds(delay.TotalSeconds);

        // Cap at max delay
        return delayTimeSpan > configuration.MaxRetryDelay
            ? configuration.MaxRetryDelay
            : delayTimeSpan;
    }

    private void MarkAsFailedPermanently(OutboxMessage message, string error)
    {
        message.Status = OutboxMessageStatus.Failed;
        message.LastError = error;
        message.NextRetryAt = null; 
    }
    
    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>
        {
            ["EventType"] = message.EventType,
            ["MessageId"] = message.Id.ToString(),
            ["OccurredAt"] = message.OccurredAt.ToString("O")
        };
        
        if (message.CorrelationId.HasValue)
        {
            headers["CorrelationId"] = message.CorrelationId.Value.ToString();
        }

        await messageTransport.SendAsync(message.Topic, message.EventType, message.Payload, headers, ct);
    }
}