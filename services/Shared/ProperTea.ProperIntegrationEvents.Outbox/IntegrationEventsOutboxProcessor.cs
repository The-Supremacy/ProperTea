using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public interface IIntegrationEventsOutboxProcessor
{
    Task ProcessOutboxMessagesAsync(int batchSize = 10, CancellationToken cancellationToken = default);
}

public class IntegrationEventsOutboxProcessor(
    IOutboxMessagesService messagesService,
    IExternalIntegrationEventPublisher eventPublisher,
    IIntegrationEventTypeResolver typeResolver,
    OutboxConfiguration configuration,
    ILogger<IntegrationEventsOutboxProcessor> logger)
    : IIntegrationEventsOutboxProcessor
{
    public async Task ProcessOutboxMessagesAsync(int batchSize = 10, CancellationToken ct = default)
    {
        var messages = (await messagesService.GetPendingOutboxMessagesAsync(batchSize, ct)).ToList();

        foreach (var message in messages)
        {
            // Skip if not ready for retry
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
                        await eventPublisher.PublishAsync(message.Topic, integrationEvent, ct);
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
                    // Calculate next retry time with exponential backoff
                    var delay = CalculateRetryDelay(message.RetryCount);
                    message.NextRetryAt = DateTime.UtcNow.Add(delay);
                    message.Status = OutboxMessageStatus.Pending; // Keep pending for retry

                    logger.LogInformation(
                        "Message {MessageId} will retry in {Delay} (attempt {Attempt}/{MaxAttempts})",
                        message.Id, delay, message.RetryCount, configuration.MaxRetryAttempts);
                }
            }

            await messagesService.SaveMessageAsync(message, ct);
        }
    }
    
    private bool IsReadyForRetry(OutboxMessage message)
    {
        // First attempt or retry time reached
        return message.NextRetryAt == null || message.NextRetryAt <= DateTime.UtcNow;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: delay = InitialDelay * (Multiplier ^ retryCount)
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
        message.NextRetryAt = null; // No more retries
    }
}