using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public class ServiceBusMessageProcessor(
    IServiceProvider serviceProvider,
    IIntegrationEventTypeResolver typeResolver,
    ILogger<ServiceBusMessageProcessor> logger)
    : IServiceBusMessageProcessor
{
    public async Task ProcessMessageAsync(
        ServiceBusReceivedMessage message,
        IMessageActions messageActions,
        CancellationToken ct = default)
    {
        var correlationId = message.CorrelationId;
        var messageId = message.MessageId;
        var eventType = message.ApplicationProperties.GetValueOrDefault("EventType")?.ToString()
                        ?? message.Subject;

        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = messageId,
            ["CorrelationId"] = correlationId ?? "null",
            ["EventType"] = eventType
        });

        try
        {
            logger.LogInformation("Processing message from ServiceBus");
            
            var type = typeResolver.ResolveType(eventType);
            if (type == null)
            {
                logger.LogWarning("Unknown event type '{EventType}'. Dead-lettering message.", eventType);
                await messageActions.DeadLetterAsync(message, "UnknownEventType", $"Event type '{eventType}' is not registered", ct);
                return;
            }
            
            IntegrationEvent? integrationEvent;
            try
            {
                var payload = message.Body.ToString();
                integrationEvent = JsonSerializer.Deserialize(payload, type) as IntegrationEvent;
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize message. Dead-lettering.");
                await messageActions.DeadLetterAsync(message, "DeserializationError", ex.Message, ct);
                return;
            }

            if (integrationEvent == null)
            {
                logger.LogWarning("Deserialization returned null. Dead-lettering message.");
                await messageActions.DeadLetterAsync(message, "DeserializationError", "Payload deserialization returned null", ct);
                return;
            }
            
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(type);
            var handler = serviceProvider.GetService(handlerType);

            if (handler == null)
            {
                logger.LogWarning("No handler registered for event type '{EventType}'. Dead-lettering message.", eventType);
                await messageActions.DeadLetterAsync(message, "NoHandler", $"No handler registered for '{eventType}'", ct);
                return;
            }
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                logger.LogError("HandleAsync method not found on handler. Dead-lettering message.");
                await messageActions.DeadLetterAsync(message, "HandlerError", "HandleAsync method not found", ct);
                return;
            }

            await (Task)handleMethod.Invoke(handler, [integrationEvent, ct])!;
            
            await messageActions.CompleteAsync(message, ct);

            logger.LogInformation("Successfully processed message");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message. Abandoning for retry.");
            await messageActions.AbandonAsync(message, ct);
        }
    }
}
