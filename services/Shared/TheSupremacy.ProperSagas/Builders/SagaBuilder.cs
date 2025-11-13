using Polly;
using Polly.Retry;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Orchestration;

namespace TheSupremacy.ProperSagas.Builders;

public class SagaBuilder
{
    private readonly List<SagaStepDefinition> _definitions = [];
    private readonly List<SagaValidationDefinition> _validationDefinitions = [];
    private string? _idempotencyKey;
    private string? _displayName;

    public SagaBuilder AddPreValidation(
        string name,
        Func<Saga, Task<SagaValidationResult>> validateAction)
    {
        _validationDefinitions.Add(new SagaValidationDefinition
        {
            Name = name,
            ValidateAction = validateAction
        });

        return this;
    }

    public SagaBuilder AddStep(
        string name,
        Func<Saga, Task> execute,
        Func<Saga, Task>? compensate = null,
        SagaStepType type = SagaStepType.Execution,
        RetryStrategyOptions? retryOptions = null)
    {
        ResiliencePipeline? pipeline = null;

        if (retryOptions != null)
            pipeline = new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .Build();

        _definitions.Add(new SagaStepDefinition
        {
            Name = name,
            Type = type,
            ExecuteAction = execute,
            CompensationAction = compensate,
            ResiliencePipeline = pipeline
        });

        return this;
    }

    public SagaBuilder AddStepWithExponentialRetry(
        string name,
        Func<Saga, Task> execute,
        Func<Saga, Task>? compensate = null,
        SagaStepType type = SagaStepType.Execution,
        int maxRetries = 3)
    {
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = maxRetries,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
        };

        return AddStep(name, execute, compensate, type, retryOptions);
    }

    internal Dictionary<string, SagaStepDefinition> BuildStepDefinitions()
    {
        return _definitions.ToDictionary(d => d.Name);
    }

    internal List<SagaValidationDefinition> BuildValidationDefinitions()
    {
        return _validationDefinitions;
    }

    public SagaBuilder WithIdempotencyKey(string key)
    {
        _idempotencyKey = key;
        return this;
    }
    
    public SagaBuilder WithDisplayName(string name)
    {
        _displayName = name;
        return this;
    }

    internal Saga BuildSaga(string sagaType)
    {
        return new Saga
        {
            SagaType = sagaType,
            DisplayName = _displayName ?? sagaType,
            Steps = _definitions.Select(d => new SagaStep
            {
                Name = d.Name,
                Type = d.Type,
                CompensationName = d.CompensationAction != null ? d.Name : null
            }).ToList(),
            IdempotencyKey = _idempotencyKey
        };
    }
}