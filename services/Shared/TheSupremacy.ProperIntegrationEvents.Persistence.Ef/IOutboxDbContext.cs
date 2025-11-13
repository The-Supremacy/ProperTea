using Microsoft.EntityFrameworkCore;
using TheSupremacy.ProperIntegrationEvents.Outbox;

namespace TheSupremacy.ProperIntegrationEvents.Persistence.Ef;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}