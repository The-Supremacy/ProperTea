using Wolverine;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Extensions;

public static class WolverineExtensions
{
    public static void PublishIntegrationEvent<T>(
        this WolverineOptions opts,
        string exchange)
    {
        _ = opts.PublishMessage<T>()
            .ToRabbitTopics(exchange)
            .UseDurableOutbox();
    }
}
