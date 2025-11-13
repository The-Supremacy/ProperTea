using System.Diagnostics;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Orchestration;

public static class SagaTelemetry
{
    private static readonly ActivitySource ActivitySource = new("TheSupremacy.ProperSagas");

    public static Activity? StartSagaActivity(string operation, string sagaType)
    {
        var activity = ActivitySource.StartActivity($"Saga.{operation}", ActivityKind.Server);
        activity?.SetTag("saga.type", sagaType);
        return activity;
    }

    public static Activity? StartStepActivity(string stepName, Saga saga)
    {
        var activity = ActivitySource.StartActivity($"Saga.Step.{stepName}");
        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("saga.type", saga.SagaType);
        activity?.SetTag("step.name", stepName);
        activity?.SetTag("correlation.id", saga.CorrelationId);
        return activity;
    }

    public static Activity? StartValidationActivity(string validationName, Saga saga)
    {
        var activity = ActivitySource.StartActivity($"Saga.Validation.{validationName}");
        activity?.SetTag("saga.id", saga.Id);
        activity?.SetTag("saga.type", saga.SagaType);
        activity?.SetTag("validation.name", validationName);
        activity?.SetTag("correlation.id", saga.CorrelationId);
        return activity;
    }

    public static ActivitySource GetActivitySource()
    {
        return ActivitySource;
    }
}