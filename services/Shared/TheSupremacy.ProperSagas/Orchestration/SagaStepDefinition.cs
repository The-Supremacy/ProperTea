using Polly;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Orchestration;

public class SagaStepDefinition
{
    public required string Name { get; init; }
    public SagaStepType Type { get; init; } = SagaStepType.Execution;
    public required Func<Saga, Task> ExecuteAction { get; init; }
    public Func<Saga, Task>? CompensationAction { get; init; }
    public ResiliencePipeline? ResiliencePipeline { get; init; }
}

public class SagaValidationDefinition
{
    public required string Name { get; init; }
    public required Func<Saga, Task<SagaValidationResult>> ValidateAction { get; init; }
}