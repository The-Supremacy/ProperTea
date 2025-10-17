using Microsoft.EntityFrameworkCore;

namespace ProperTea.ProperDdd.Persistence.Ef;

public class EfRepository<TEntity>(DbContext dbContext) : IRepository<TEntity> 
    where TEntity : class, IAggregateRoot
{
    protected readonly DbSet<TEntity> DbSet = dbContext.Set<TEntity>();

    protected virtual IQueryable<TEntity> IncludeRelations(IQueryable<TEntity> query)
    {
        return query;
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var query = IncludeRelations(DbSet.AsQueryable());
        return await query.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public virtual async Task AddAsync(TEntity aggregate, CancellationToken ct = default)
    {
        await DbSet.AddAsync(aggregate, ct);
    }

    public virtual void Update(TEntity aggregate)
    {
        DbSet.Update(aggregate);
    }

    public virtual void Delete(TEntity aggregate)
    {
        DbSet.Remove(aggregate);
    }
}