using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Services;

public class SagaBackgroundProcessor(
    IServiceProvider serviceProvider,
    IOptions<SagaOptions> options,
    ILogger<SagaBackgroundProcessor> logger)
{
    public async Task ProcessStaleSagasAsync()
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();
        var resumeService = serviceProvider.GetRequiredService<ISagaResumeService>();

        var staleSagas = await repository.FindSagasNeedingResumptionAsync(options.Value.LockTimeout);

        logger.LogInformation("Found {Count} stale sagas to resume", staleSagas.Count);

        foreach (var sagaId in staleSagas.Select(s => s.Id))
            try
            {
                logger.LogInformation("Resuming stale saga {SagaId}", sagaId);
                await resumeService.ResumeAsync(sagaId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resume stale saga {SagaId}", sagaId);
            }
    }

    public async Task ProcessTimedOutSagasAsync()
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();
        var resumeService = serviceProvider.GetRequiredService<ISagaResumeService>();

        var timedOutSagas = await repository.FindTimedOutSagasAsync();

        logger.LogInformation("Found {Count} timed out sagas", timedOutSagas.Count);

        foreach (var sagaId in timedOutSagas.Select(s => s.Id))
            try
            {
                logger.LogInformation("Processing timed out saga {SagaId}", sagaId);
                await resumeService.ResumeAsync(sagaId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process timed out saga {SagaId}", sagaId);
            }
    }

    public async Task ProcessScheduledSagasAsync()
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();
        var resumeService = serviceProvider.GetRequiredService<ISagaResumeService>();

        var scheduledSagas = await repository.FindScheduledSagasAsync(DateTime.UtcNow);

        logger.LogInformation("Found {Count} scheduled sagas ready to execute", scheduledSagas.Count);

        foreach (var sagaId in scheduledSagas.Select(s => s.Id))
            try
            {
                logger.LogInformation("Starting scheduled saga {SagaId}", sagaId);
                await resumeService.ResumeAsync(sagaId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start scheduled saga {SagaId}", sagaId);
            }
    }

    public async Task ProcessAllAsync()
    {
        await ProcessScheduledSagasAsync();
        await ProcessStaleSagasAsync();
        await ProcessTimedOutSagasAsync();
    }
}