using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Services;

public interface ISagaBackgroundProcessor
{
    Task ProcessStaleSagasAsync();
    Task ProcessScheduledSagasAsync();
    Task ProcessAllAsync();
}

public class SagaBackgroundProcessor(
    IServiceProvider serviceProvider,
    IOptions<SagaOptions> options,
    ILogger<SagaBackgroundProcessor> logger) 
    : ISagaBackgroundProcessor
{
    public async Task ProcessStaleSagasAsync()
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();
        var staleSagas = await repository.FindSagasNeedingResumptionAsync(options.Value.LockTimeout);

        logger.LogInformation("Found {Count} stale sagas to resume", staleSagas.Count);

        await ResumeSagas(staleSagas);
    }

    public async Task ProcessScheduledSagasAsync()
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();
        var scheduledSagas = await repository.FindScheduledSagasAsync(DateTime.UtcNow);

        logger.LogInformation("Found {Count} scheduled sagas ready to execute", scheduledSagas.Count);

        await ResumeSagas(scheduledSagas);
    }

    public async Task ProcessAllAsync()
    {
        await Task.WhenAll(
            ProcessScheduledSagasAsync(),
            ProcessStaleSagasAsync());
    }
    
    private async Task ResumeSagas(IEnumerable<Saga> sagas)
    {
        // TODO: consider batching
        var resumeService = serviceProvider.GetRequiredService<ISagaResumeService>();
        foreach (var sagaId in sagas.Select(s => s.Id))
            try
            {
                logger.LogInformation("Starting/resuming saga {SagaId}", sagaId);
                await resumeService.ResumeAsync(sagaId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start/resume saga {SagaId}", sagaId);
            }
    }
}