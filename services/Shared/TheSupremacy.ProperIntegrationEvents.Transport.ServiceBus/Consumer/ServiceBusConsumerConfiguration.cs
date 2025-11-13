namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public class ServiceBusConsumerConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxConcurrentMessages { get; set; } = 10;
    public int PrefetchCount { get; set; } = 0;
}