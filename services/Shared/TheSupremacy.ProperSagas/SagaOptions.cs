namespace TheSupremacy.ProperSagas;

public class SagaOptions
{
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SagaTimeout { get; set; } = TimeSpan.FromHours(24);
    public int MaxConcurrentSagas { get; set; } = 10;
    public int BackgroundProcessingIntervalSeconds { get; set; } = 60;
}