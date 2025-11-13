using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Exceptions;
using TheSupremacy.ProperSagas.Orchestration;

namespace TheSupremacy.ProperSagas.Services;

public interface ISagaResumeService
{
    Task<Saga> ResumeAsync(Guid sagaId);
    Task<Saga> ResetTimeoutAndResumeAsync(Guid sagaId, TimeSpan? newTimeout = null);
    Task<List<Saga>> GetFailedSagasAsync();
}

public class SagaResumeService(
    IServiceProvider serviceProvider,
    SagaRegistry registry,
    ILogger<ISagaResumeService> logger) : ISagaResumeService
{
    public async Task<Saga> ResumeAsync(Guid sagaId)
    {
        var repository = serviceProvider.GetRequiredService<ISagaRepository>();

        var saga = await repository.GetByIdAsync(sagaId);
        if (saga == null)
            throw new SagaNotFoundException(sagaId);

        var orchestratorType = registry.GetOrchestratorType(saga.SagaType);
        if (orchestratorType == null)
            throw new InvalidOperationException($"No orchestrator registered for saga type {saga.SagaType}");

        logger.LogInformation("Resuming saga {SagaId} of type {SagaType}", sagaId, saga.SagaType);

        var orchestrator = (SagaOrchestratorBase)serviceProvider.GetRequiredService(orchestratorType);
        return await orchestrator.ResumeAsync(sagaId);
    }

    public async Task<Saga> ResetTimeoutAndResumeAsync(Guid sagaId, TimeSpan? newTimeout = null)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        var saga = await repository.GetByIdAsync(sagaId);
        if (saga == null)
            throw new InvalidOperationException($"Saga {sagaId} not found");

        logger.LogInformation("Resetting timeout for saga {SagaId}", sagaId);

        // Reset timeout
        if (newTimeout.HasValue)
        {
            saga.SetTimeout(newTimeout.Value);
        }
        else
        {
            // Reset with same duration from now
            if (saga.Timeout.HasValue)
                saga.SetTimeout(saga.Timeout.Value);
        }

        await repository.TryUpdateAsync(saga);

        return await ResumeAsync(sagaId);
    }

    public async Task<List<Saga>> GetFailedSagasAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        // You'll need to add this method to ISagaRepository
        return await repository.FindFailedSagasAsync();
    }
}