using Azure.Messaging.ServiceBus;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public interface IMessageActions
{
    Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken ct = default);
    Task DeadLetterAsync(ServiceBusReceivedMessage message, string reason, string description, CancellationToken ct = default);
    Task AbandonAsync(ServiceBusReceivedMessage message, CancellationToken ct = default);
}