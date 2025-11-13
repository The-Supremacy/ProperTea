using Azure.Messaging.ServiceBus;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public interface IServiceBusMessageProcessor
{
    Task ProcessMessageAsync(
        ServiceBusReceivedMessage message,
        IMessageActions messageActions,
        CancellationToken ct = default);
}