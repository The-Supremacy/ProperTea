using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using TheSupremacy.ProperSagas.Builders;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Exceptions;

namespace TheSupremacy.ProperSagas.Orchestration;

public abstract class SagaOrchestratorBase(
    ISagaRepository sagaRepository,
    IOptions<SagaOptions> options,
    ILogger logger)
{
    private readonly SagaOptions _options = options.Value;
    private readonly Guid _workerToken = Guid.NewGuid();
    private Dictionary<string, SagaStepDefinition>? _stepDefinitions;
    private List<SagaValidationDefinition>? _validationDefinitions;

    protected abstract string SagaType { get; }

    private Dictionary<string, SagaStepDefinition> StepDefinitions
    {
        get
        {
            if (_stepDefinitions != null)
                return _stepDefinitions;

            var builder = new SagaBuilder();
            DefineSaga(builder);
            _stepDefinitions = builder.BuildStepDefinitions();

            return _stepDefinitions;
        }
    }

    private List<SagaValidationDefinition> ValidationDefinitions
    {
        get
        {
            if (_validationDefinitions != null)
                return _validationDefinitions;

            var builder = new SagaBuilder();
            DefineSaga(builder);
            _validationDefinitions = builder.BuildValidationDefinitions();

            return _validationDefinitions;
        }
    }

    protected virtual bool AllowCancellation => false;

    private Saga BuildSaga()
    {
        var builder = new SagaBuilder();
        DefineSaga(builder);
        return builder.BuildSaga(SagaType);
    }

    protected abstract void DefineSaga(SagaBuilder builder);

    protected virtual TimeSpan GetSagaTimeout()
    {
        return _options.SagaTimeout;
    }

    protected virtual Task OnCancellationRequestedAsync(Saga saga)
    {
        logger.LogInformation("Saga {SagaId} cancellation requested", saga.Id);
        return Task.CompletedTask;
    }

    protected virtual Task OnPonrFailureAsync(Saga saga, string failedStep)
    {
        logger.LogCritical(
            "Saga {SagaId} failed at PONR step {StepName}. Manual intervention required.",
            saga.Id, failedStep);
        return Task.CompletedTask;
    }

    public async Task<Saga> StartAsync(object? initialData = null)
    {
        using var activity = SagaTelemetry.StartSagaActivity("Start", SagaType);

        var saga = BuildSaga();
        saga.SetTimeout(GetSagaTimeout());
        saga.CorrelationId = Activity.Current?.RootId ?? Guid.NewGuid().ToString();
        saga.TraceId = Activity.Current?.Id;

        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("correlation.id", saga.CorrelationId);

        if (initialData != null)
            saga.SetData("initialData", initialData);
        var validationResult = await ValidateSagaAsync(saga);
        if (!validationResult.IsValid)
        {
            saga.MarkAsFailed(validationResult.ErrorMessage!);
            await sagaRepository.AddAsync(saga);
            activity?.SetStatus(ActivityStatusCode.Error, validationResult.ErrorMessage);
            return saga;
        }

        await sagaRepository.AddAsync(saga);
        if (!saga.TryAcquireLock(_workerToken, _options.LockTimeout))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to acquire lock");
            throw new InvalidOperationException("Failed to acquire saga lock");
        }

        try
        {
            logger.LogInformation("Starting saga {SagaId}", saga.Id);

            saga.MarkAsRunning();
            await sagaRepository.TryUpdateAsync(saga);

            await ExecuteStepsAsync(saga);

            activity?.SetStatus(saga.Status == SagaStatus.Completed
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);

            return saga;
        }
        finally
        {
            saga.ReleaseLock(_workerToken);
            await sagaRepository.TryUpdateAsync(saga);
        }
    }

    public async Task<Saga> ScheduleAsync(object? initialData = null, DateTime? scheduledFor = null)
    {
        using var activity = SagaTelemetry.StartSagaActivity("Schedule", SagaType);

        var saga = BuildSaga();
        saga.SetTimeout(GetSagaTimeout());
        saga.CorrelationId = Activity.Current?.RootId ?? Guid.NewGuid().ToString();
        saga.TraceId = Activity.Current?.Id;

        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("correlation.id", saga.CorrelationId);

        if (initialData != null)
            saga.SetData("initialData", initialData);

        var validationResult = await ValidateSagaAsync(saga);
        if (!validationResult.IsValid)
        {
            saga.MarkAsFailed(validationResult.ErrorMessage!);
            await sagaRepository.AddAsync(saga);
            activity?.SetStatus(ActivityStatusCode.Error, validationResult.ErrorMessage);
            return saga;
        }

        saga.Schedule(scheduledFor);
        await sagaRepository.AddAsync(saga);

        logger.LogInformation("Scheduled saga {SagaId} for execution", saga.Id);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return saga;
    }

    public async Task<Saga> ResumeAsync(Guid sagaId)
    {
        using var activity = SagaTelemetry.StartSagaActivity("Resume", SagaType);

        var saga = await sagaRepository.GetByIdAsync(sagaId);
        if (saga == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Saga not found");
            throw new SagaNotFoundException(sagaId);
        }

        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("correlation.id", saga.CorrelationId);

        if (saga.Status is SagaStatus.Completed or SagaStatus.Compensated or SagaStatus.FailedAfterPonr)
        {
            logger.LogInformation("Saga {SagaId} already finished with status {Status}",
                sagaId, saga.Status);
            return saga;
        }

        if (!saga.TryAcquireLock(_workerToken, _options.LockTimeout))
        {
            logger.LogWarning("Failed to acquire lock for saga {SagaId}", sagaId);
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to acquire lock");
            return saga;
        }

        try
        {
            logger.LogInformation("Resuming saga {SagaId} from status {Status}", sagaId, saga.Status);

            saga.MarkAsRunning();
            if (!await sagaRepository.TryUpdateAsync(saga))
            {
                logger.LogWarning("Failed to update saga {SagaId} - version conflict", sagaId);
                saga.ReleaseLock(_workerToken);
                activity?.SetStatus(ActivityStatusCode.Error, "Version conflict");
                return saga;
            }

            await ExecuteStepsAsync(saga);

            activity?.SetStatus(saga.Status == SagaStatus.Completed
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);

            return saga;
        }
        finally
        {
            saga.ReleaseLock(_workerToken);
            await sagaRepository.TryUpdateAsync(saga);
        }
    }

    private async Task ExecuteStepsAsync(Saga saga)
    {
        if (saga.IsTimedOut())
        {
            logger.LogError("Saga {SagaId} timed out", saga.Id);
            saga.MarkAsFailed("Saga execution timed out");
            saga.ReleaseLock(_workerToken);
            await sagaRepository.TryUpdateAsync(saga);
            return;
        }

        var ponrReached = false;

        foreach (var step in saga.Steps)
        {
            if (step.Status == SagaStepStatus.Completed)
            {
                logger.LogInformation("Skipping completed step {StepName}", step.Name);

                if (step.Type == SagaStepType.PointOfNoReturn)
                    ponrReached = true;

                continue;
            }

            if (AllowCancellation && saga.IsCancellationRequested && !ponrReached)
            {
                logger.LogInformation("Saga {SagaId} cancelled before step {StepName}",
                    saga.Id, step.Name);

                await OnCancellationRequestedAsync(saga);
                await CompensateAsync(saga);
                return;
            }

            var definition = StepDefinitions[step.Name];
            var success =
                await ExecuteStepAsync(saga, step.Name, definition.ExecuteAction, definition.ResiliencePipeline);

            if (!success)
            {
                if (ponrReached)
                {
                    logger.LogCritical("Saga {SagaId} failed after PONR at step {StepName}",
                        saga.Id, step.Name);

                    saga.MarkAsFailedAfterPonr($"Failed at step {step.Name} after point of no return");
                    saga.ReleaseLock(_workerToken);
                    await sagaRepository.TryUpdateAsync(saga);

                    await OnPonrFailureAsync(saga, step.Name);
                    return;
                }

                await CompensateAsync(saga);
                return;
            }

            if (step.Type == SagaStepType.PointOfNoReturn)
                ponrReached = true;
        }

        saga.MarkAsCompleted();
        saga.ReleaseLock(_workerToken);
        await sagaRepository.TryUpdateAsync(saga);
    }

    private async Task<bool> ExecuteStepAsync(
        Saga saga,
        string stepName,
        Func<Saga, Task> action,
        ResiliencePipeline? pipeline)
    {
        using var activity = SagaTelemetry.StartStepActivity(stepName, saga);

        try
        {
            logger.LogInformation("Executing step: {StepName} for saga {SagaId} (CorrelationId: {CorrelationId})",
                stepName, saga.Id, saga.CorrelationId);

            saga.MarkStepAsRunning(stepName);
            await sagaRepository.TryUpdateAsync(saga);

            if (pipeline != null)
                await pipeline.ExecuteAsync(async ct => await action(saga), CancellationToken.None);
            else
                await action(saga);

            saga.MarkStepAsCompleted(stepName);
            await sagaRepository.TryUpdateAsync(saga);

            logger.LogInformation("Completed step: {StepName} for saga {SagaId}", stepName, saga.Id);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed step: {StepName} for saga {SagaId} (CorrelationId: {CorrelationId})",
                stepName, saga.Id, saga.CorrelationId);

            saga.MarkStepAsFailed(stepName, ex.Message);
            saga.MarkAsFailed($"Step {stepName} failed: {ex.Message}");
            await sagaRepository.TryUpdateAsync(saga);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            return false;
        }
    }

    private async Task<SagaValidationResult> ValidateSagaAsync(Saga saga)
    {
        foreach (var validation in ValidationDefinitions)
        {
            using var validationActivity = SagaTelemetry.StartValidationActivity(validation.Name, saga);

            logger.LogInformation("Running validation: {ValidationName} for saga {SagaId}",
                validation.Name, saga.Id);

            try
            {
                var result = await validation.ValidateAction(saga);

                if (!result.IsValid)
                {
                    logger.LogWarning("Validation {ValidationName} failed for saga {SagaId}: {Error}",
                        validation.Name, saga.Id, result.ErrorMessage);

                    validationActivity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                    return SagaValidationResult.Failure(
                        $"Validation '{validation.Name}' failed: {result.ErrorMessage}");
                }

                validationActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Validation {ValidationName} threw exception for saga {SagaId}",
                    validation.Name, saga.Id);

                validationActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                validationActivity?.AddException(ex);

                return SagaValidationResult.Failure($"Validation '{validation.Name}' threw exception: {ex.Message}");
            }
        }

        return SagaValidationResult.Success();
    }

    private async Task CompensateAsync(Saga saga)
    {
        using var activity = SagaTelemetry.StartSagaActivity("Compensate", saga.SagaType);
        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("correlation.id", saga.CorrelationId);

        logger.LogInformation("Starting automatic compensation for saga {SagaId}", saga.Id);

        saga.MarkAsCompensating();
        await sagaRepository.TryUpdateAsync(saga);

        var stepsToCompensate = saga.GetStepsNeedingCompensation().ToList();

        if (stepsToCompensate.Count == 0)
        {
            logger.LogInformation("No steps need compensation for saga {SagaId}", saga.Id);
            saga.MarkAsCompensated();
            saga.ReleaseLock(_workerToken);
            await sagaRepository.TryUpdateAsync(saga);
            return;
        }

        foreach (var step in stepsToCompensate)
        {
            var definition = StepDefinitions[step.Name];
            if (definition.CompensationAction == null)
                continue;

            try
            {
                logger.LogInformation("Compensating step {StepName} for saga {SagaId}",
                    step.Name, saga.Id);

                await definition.CompensationAction(saga);

                step.Status = SagaStepStatus.Compensated;
                await sagaRepository.TryUpdateAsync(saga);

                logger.LogInformation("Successfully compensated step {StepName} for saga {SagaId}",
                    step.Name, saga.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Compensation failed for step {StepName} in saga {SagaId}",
                    step.Name, saga.Id);

                step.ErrorMessage = $"Compensation failed: {ex.Message}";
                await sagaRepository.TryUpdateAsync(saga);
            }
        }

        saga.MarkAsCompensated();
        saga.ReleaseLock(_workerToken);
        await sagaRepository.TryUpdateAsync(saga);

        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}