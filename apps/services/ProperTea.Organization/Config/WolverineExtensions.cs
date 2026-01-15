using Wolverine;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Config;

/// <summary>
/// Helper extensions to reduce boilerplate for message configuration.
/// Supports both RabbitMQ (now) and Azure Service Bus (future).
/// </summary>
public static class WolverineExtensions
{
    /// <summary>
    /// Configure an integration event for RabbitMQ publication with durable outbox.
    /// </summary>
    public static void PublishIntegrationEvent<T>(
        this WolverineOptions opts,
        string exchange,
        string messageIdentity) where T : class
    {
        _ = opts.PublishMessage<T>()
            .ToRabbitExchange(exchange)
            .CustomizeOutgoing(env => env.MessageType = messageIdentity)
            .UseDurableOutbox();
    }

    // TODO: Add ServiceBus variant when cloud deployment is ready
    // public static void PublishIntegrationEventToServiceBus<T>(...)
}
