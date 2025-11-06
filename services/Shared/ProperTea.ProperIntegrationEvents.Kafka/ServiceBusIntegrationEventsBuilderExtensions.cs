using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents.Kafka;

public static class KafkaIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder AddKafka(
        this IntegrationEventsBuilder builder,
        Action<KafkaConfiguration> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<IExternalIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        return builder;
    }

    public static IntegrationEventsBuilder UseDirectKafka(
        this IntegrationEventsBuilder builder,
        Action<KafkaConfiguration> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        return builder;
    }

    public static IntegrationEventsBuilder AddKafka(
        this IntegrationEventsBuilder builder,
        string bootstrapServers)
    {
        return builder.AddKafka(config => { config.BootstrapServers = bootstrapServers; });
    }

    public static IntegrationEventsBuilder UseDirectKafka(
        this IntegrationEventsBuilder builder,
        string bootstrapServers)
    {
        return builder.UseDirectKafka(config => { config.BootstrapServers = bootstrapServers; });
    }
}