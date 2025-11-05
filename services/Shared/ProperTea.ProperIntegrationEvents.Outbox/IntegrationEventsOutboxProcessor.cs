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
    ILogger<IntegrationEventsOutboxProcessor> logger)
    : IIntegrationEventsOutboxProcessor
{
    public async Task ProcessOutboxMessagesAsync(int batchSize = 10, CancellationToken ct = default)
    {
        var messages = (await messagesService.GetPendingOutboxMessagesAsync(batchSize, ct)).ToList();

        foreach (var message in messages)
        {
            try
            {
                var eventType = typeResolver.ResolveType(message.EventType);
                if (eventType is null)
                {
                    logger.LogWarning(
                        "Unknown event type '{EventType}' for outbox message {MessageId}. Marking as failed.",
                        message.EventType, message.Id);
                    message.Status = OutboxMessageStatus.Failed;
                    message.LastError = $"Unknown event type: {message.EventType}";
                }
                else
                {
                    var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType) as IntegrationEvent;

                    if (integrationEvent is null)
                    {
                        logger.LogWarning(
                            "Could not deserialize payload for outbox message {MessageId}. Marking as failed.",
                            message.Id);
                        message.Status = OutboxMessageStatus.Failed;
                        message.LastError = "Payload deserialization returned null.";
                    }
                    else
                    {
                        await eventPublisher.PublishAsync(message.Topic, integrationEvent, ct);
                        message.Status = OutboxMessageStatus.Published;
                        message.PublishedAt = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);
                message.Status = OutboxMessageStatus.Failed;
                message.RetryCount++;
                message.LastError = ex.Message;
            }

            await messagesService.SaveMessageAsync(message, ct);
        }
    }
}