namespace TheSupremacy.ProperDomain;

public interface IDomainUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}