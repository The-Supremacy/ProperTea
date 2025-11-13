using Azure.Messaging.ServiceBus;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

internal class ServiceBusMessageActions(ProcessMessageEventArgs args) : IMessageActions
{
    public Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken ct = default)
    {
        return args.CompleteMessageAsync(message, ct);
    }

    public Task DeadLetterAsync(ServiceBusReceivedMessage message, string reason, string description, CancellationToken ct = default)
    {
        return args.DeadLetterMessageAsync(message, reason, description, cancellationToken: ct);
    }

    public Task AbandonAsync(ServiceBusReceivedMessage message, CancellationToken ct = default)
    {
        return args.AbandonMessageAsync(message, cancellationToken: ct);
    }
}