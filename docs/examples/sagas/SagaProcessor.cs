using Microsoft.Extensions.Hosting;
using ProperTea.ProperSagas;

namespace Examples.Sagas;

/// <summary>
/// Example background service that polls for waiting sagas and resumes them
/// This is a reference example for local development - for production use Azure Durable Functions
/// </summary>
public class SagaProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

    public SagaProcessor(
        IServiceProvider serviceProvider,
        ILogger<SagaProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SagaProcessor started. Polling every {Interval} seconds.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingSagasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending sagas");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("SagaProcessor stopped.");
    }

    private async Task ProcessPendingSagasAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        // Find sagas waiting for callback
        var pendingSagaIds = await repository.FindByStatusAsync(SagaStatus.WaitingForCallback);

        if (pendingSagaIds.Any())
        {
            _logger.LogInformation("Found {Count} sagas waiting for callback", pendingSagaIds.Count);
        }

        foreach (var sagaId in pendingSagaIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Resolve the appropriate orchestrator for this saga
                // This is a simplified example - you may need a saga type registry
                var orchestrator = scope.ServiceProvider.GetRequiredService<GDPRDeletionOrchestrator>();
                
                _logger.LogInformation("Resuming saga {SagaId}", sagaId);
                await orchestrator.ResumeAsync(sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming saga {SagaId}", sagaId);
            }
        }
    }
}

