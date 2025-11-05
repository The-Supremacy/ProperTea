using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public static class OutboxIntegrationEventsBuilderExtensions
{
    public static OutboxBuilder UseOutbox(
        this IntegrationEventsBuilder builder,
        Action<OutboxBuilder>? outboxConfiguration = null)
    {
        builder.Services.TryAddScoped<IIntegrationEventsOutboxProcessor, IntegrationEventsOutboxProcessor>();

        var outboxBuilder = new OutboxBuilder(builder.Services);
        outboxConfiguration?.Invoke(outboxBuilder);

        return outboxBuilder;
    }
}