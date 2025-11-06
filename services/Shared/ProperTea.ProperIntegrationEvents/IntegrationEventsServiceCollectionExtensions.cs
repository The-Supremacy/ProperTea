using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents;

public static class IntegrationEventsServiceCollectionExtensions
{
    public static IntegrationEventsBuilder AddProperIntegrationEvents(this IServiceCollection services,
        Action<IntegrationEventsBuilder>? configure = null)
    {
        var builder = new IntegrationEventsBuilder(services);
        configure?.Invoke(builder);

        services.AddSingleton<IIntegrationEventTypeResolver>(sp =>
            new IntegrationEventTypeResolver(builder.EventTypes));

        return builder;
    }
}