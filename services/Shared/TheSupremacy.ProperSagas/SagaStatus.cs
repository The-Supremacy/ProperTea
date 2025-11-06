namespace TheSupremacy.ProperSagas;

public enum SagaStatus
{
    NotStarted = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Compensating = 4,
    Compensated = 5,
    ValidationFailed = 6,
    WaitingForCallback = 7
}