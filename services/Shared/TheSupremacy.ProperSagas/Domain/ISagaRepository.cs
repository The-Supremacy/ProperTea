namespace TheSupremacy.ProperSagas.Domain;

public interface ISagaRepository
{
    Task<Saga?> GetByIdAsync(Guid sagaId);
    Task AddAsync(Saga saga);
    Task<bool> TryUpdateAsync(Saga saga);
    Task<List<Saga>> FindSagasNeedingResumptionAsync(TimeSpan lockTimeout);
    Task<List<Saga>> FindTimedOutSagasAsync();
    Task<List<Saga>> FindFailedSagasAsync();
    Task<List<Saga>> FindSagasByCorrelationIdAsync(string correlationId);
    Task<List<Saga>> FindScheduledSagasAsync(DateTime asOf);
}