namespace TheSupremacy.ProperDomain;

public interface IDomainRepository<T> where T : IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T aggregate, CancellationToken ct = default);
    void Update(T aggregate);
    void Delete(T aggregate);
}