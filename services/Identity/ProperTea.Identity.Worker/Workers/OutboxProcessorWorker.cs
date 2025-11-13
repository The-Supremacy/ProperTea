using Microsoft.Extensions.Options;
using TheSupremacy.ProperIntegrationEvents.Outbox;

namespace ProperTea.Identity.Worker.Workers;

public class OutboxProcessorWorker(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessorWorker> logger,
    IOptions<OutboxProcessorOptions>? options = null)
    : BackgroundService
{
    private readonly OutboxProcessorOptions _options = options?.Value ?? new OutboxProcessorOptions();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_options.PollingIntervalSeconds * 1000, stoppingToken);
        }

        logger.LogInformation("Outbox Processor Worker stopping...");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxProcessor = scope.ServiceProvider.GetRequiredService<IIntegrationEventsOutboxProcessor>();

        logger.LogDebug("Checking for pending outbox messages...");

        // Use the existing IntegrationEventsOutboxProcessor to handle all the logic
        await outboxProcessor.ProcessOutboxMessagesAsync(_options.BatchSize, cancellationToken);
    }
}

/// <summary>
///     Configuration options for the outbox processor
/// </summary>
public class OutboxProcessorOptions
{
    /// <summary>
    ///     How often to poll for new messages (in seconds)
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    ///     How many messages to process in each batch
    /// </summary>
    public int BatchSize { get; set; } = 10;
}