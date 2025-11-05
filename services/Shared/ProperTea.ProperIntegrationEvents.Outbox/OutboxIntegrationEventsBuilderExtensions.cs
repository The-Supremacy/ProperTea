using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public static class OutboxIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder UseOutbox(
        this IntegrationEventsBuilder builder,
        Action<OutboxBuilder> outboxConfiguration)
    {
        builder.Services.TryAddScoped<IIntegrationEventsOutboxProcessor, IntegrationEventsOutboxProcessor>();

        var outboxBuilder = new OutboxBuilder(builder.Services);
        outboxConfiguration(outboxBuilder);

        return builder;
    }
}