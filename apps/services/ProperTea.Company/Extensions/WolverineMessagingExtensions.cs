using Wolverine;
using Wolverine.RabbitMQ;

namespace ProperTea.Company.Extensions;

public static class WolverineMessagingExtensions
{
    public static void PublishIntegrationEvent<T>(
        this WolverineOptions opts,
        string exchangeName)
    {
        _ = opts.PublishMessage<T>()
            .ToRabbitTopics(exchangeName)
            .UseDurableOutbox();
    }
}
