namespace ProperTea.ProperSagas.Ef;

/// <summary>
///     Database entity for storing saga state
/// </summary>
public class SagaEntity
{
    public Guid Id { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SagaData { get; set; } = "{}";
    public string Steps { get; set; } = "[]";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}