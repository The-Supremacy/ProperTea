using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents.ServiceBus;

public static class ServiceBusIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder AddServiceBus(
        this IntegrationEventsBuilder builder,
        Action<ServiceBusConfiguration> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<IExternalIntegrationEventPublisher, ServiceBusIntegrationEventPublisher>();
        return builder;
    }

    public static IntegrationEventsBuilder UseDirectServiceBus(
        this IntegrationEventsBuilder builder,
        Action<ServiceBusConfiguration> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<IIntegrationEventPublisher, ServiceBusIntegrationEventPublisher>();
        return builder;
    }

    public static IntegrationEventsBuilder AddServiceBus(
        this IntegrationEventsBuilder builder,
        string connectionString)
    {
        return builder.AddServiceBus(config => { config.ConnectionString = connectionString; });
    }

    public static IntegrationEventsBuilder UseDirectServiceBus(
        this IntegrationEventsBuilder builder,
        string connectionString)
    {
        return builder.UseDirectServiceBus(config => { config.ConnectionString = connectionString; });
    }
}