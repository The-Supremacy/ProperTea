using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public class DbContextOutboxMessagesService : IOutboxMessagesService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DbContextOutboxMessagesService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingOutboxMessagesAsync(
        int batchSize = 10, CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        return await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task SaveMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        dbContext.OutboxMessages.Update(message);

        await ((DbContext)dbContext).SaveChangesAsync(ct);
    }
}