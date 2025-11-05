using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public static class IntegrationEventsServiceCollectionExtensions
{
    public static IntegrationEventsBuilder AddProperIntegrationEvents(this IServiceCollection services)
    {
        var builder = new IntegrationEventsBuilder(services);
        
        services.AddSingleton<IIntegrationEventTypeResolver>(sp => 
            new IntegrationEventTypeResolver(builder.EventTypes));
            
        return builder;
    }
}