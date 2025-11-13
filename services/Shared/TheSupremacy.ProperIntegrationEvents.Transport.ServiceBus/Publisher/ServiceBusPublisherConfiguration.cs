namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Publisher;

public class ServiceBusPublisherConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MessageTimeToLive { get; set; } = TimeSpan.FromDays(14);
}