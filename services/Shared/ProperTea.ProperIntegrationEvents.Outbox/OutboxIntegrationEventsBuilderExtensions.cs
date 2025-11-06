using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public static class OutboxIntegrationEventsBuilderExtensions
{
    public static OutboxBuilder AddOutbox(
        this IntegrationEventsBuilder builder,
        Action<OutboxBuilder>? outboxConfiguration = null)
    {
        builder.Services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<OutboxConfiguration>>().Value);
        builder.Services.TryAddScoped<IIntegrationEventsOutboxProcessor, IntegrationEventsOutboxProcessor>();

        var outboxBuilder = new OutboxBuilder(builder.Services);
        outboxConfiguration?.Invoke(outboxBuilder);
        
        return outboxBuilder;
    }
}