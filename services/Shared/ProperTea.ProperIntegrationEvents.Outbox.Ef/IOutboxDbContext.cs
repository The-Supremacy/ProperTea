using Microsoft.EntityFrameworkCore;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}