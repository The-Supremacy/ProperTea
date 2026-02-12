using Wolverine;
using Wolverine.RabbitMQ;

namespace ProperTea.Property.Extensions;

public static class WolverineMessagingExtensions
{
    public static void PublishIntegrationEvent<T>(
        this WolverineOptions opts,
        string exchangeName)
    {
        _ = opts.PublishMessage<T>().ToRabbitTopics(exchangeName).UseDurableOutbox();
    }
}
