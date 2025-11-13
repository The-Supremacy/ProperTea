namespace TheSupremacy.ProperSagas.Domain;

public record SagaValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static SagaValidationResult Success()
    {
        return new SagaValidationResult(true);
    }

    public static SagaValidationResult Failure(string error)
    {
        return new SagaValidationResult(false, error);
    }
}