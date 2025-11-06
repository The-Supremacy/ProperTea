namespace TheSupremacy.ProperSagas;

public enum SagaStepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Compensating = 4,
    Compensated = 5
}