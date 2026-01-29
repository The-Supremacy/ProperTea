using Wolverine;
using Wolverine.RabbitMQ;

namespace ProperTea.Organization.Extensions;

public static class WolverineExtensions
{
    public static void PublishIntegrationEvent<T>(
        this WolverineOptions opts,
        string exchange) where T : class
    {
        _ = opts.PublishMessage<T>()
            .ToRabbitExchange(exchange, e => e.ExchangeType = ExchangeType.Fanout)
            .UseDurableOutbox();
    }
}
