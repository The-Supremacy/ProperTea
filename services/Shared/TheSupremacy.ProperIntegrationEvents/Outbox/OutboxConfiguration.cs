namespace TheSupremacy.ProperIntegrationEvents.Outbox;

public class OutboxConfiguration
{
    public int MaxRetryAttempts { get; set; } = 5;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public double RetryDelayMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}