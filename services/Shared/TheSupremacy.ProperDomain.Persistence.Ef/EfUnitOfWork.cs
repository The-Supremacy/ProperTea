using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.Persistence.Ef;

public class EfUnitOfWork<TDbContext>(
    TDbContext dbContext,
    IDomainEventDispatcher dispatcher,
    IOptions<ProperDomainOptions> options,
    ILogger<EfUnitOfWork<TDbContext>> logger)
    : IUnitOfWork
    where TDbContext : DbContext
{
    private readonly ProperDomainOptions _options = options.Value;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Save data so the events have access to the latest state.
            var result = await dbContext.SaveChangesAsync(cancellationToken);
            
            var currentIteration = 0;
            bool hasMoreEvents;
            do
            {
                var domainEvents = CollectDomainEvents();
                ClearDomainEvents();

                foreach (var domainEvent in domainEvents)
                    dispatcher.Enqueue(domainEvent);

                await dispatcher.DispatchAllAsync(cancellationToken);

                if (dbContext.ChangeTracker.HasChanges())
                    await dbContext.SaveChangesAsync(cancellationToken);

                hasMoreEvents = CollectDomainEvents().Count != 0;
                currentIteration++;
            } while (hasMoreEvents && currentIteration < _options.MaxEventDispatchIterations);

            if (currentIteration >= _options.MaxEventDispatchIterations)
                logger.LogWarning("Too many iterations while dispatching domain events.");

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var domainEvents = dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents);
        return domainEvents.ToList();
    }

    private void ClearDomainEvents()
    {
        foreach (var entity in dbContext.ChangeTracker.Entries<IAggregateRoot>())
            entity.Entity.ClearDomainEvents();
    }
}