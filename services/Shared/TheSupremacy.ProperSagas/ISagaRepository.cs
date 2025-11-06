namespace TheSupremacy.ProperSagas;

public interface ISagaRepository
{
    Task<TSaga?> GetByIdAsync<TSaga>(Guid sagaId) where TSaga : SagaBase;
    Task SaveAsync<TSaga>(TSaga saga) where TSaga : SagaBase;
    Task UpdateAsync<TSaga>(TSaga saga) where TSaga : SagaBase;
    Task<List<Guid>> FindByStatusAsync(SagaStatus status);
}