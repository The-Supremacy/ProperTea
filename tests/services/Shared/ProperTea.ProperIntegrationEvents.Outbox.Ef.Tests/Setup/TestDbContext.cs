using Microsoft.EntityFrameworkCore;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef.Tests.Setup;

public class TestDbContext : DbContext, IOutboxDbContext
{
    public TestDbContext()
    {
    }
    
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>();
        base.OnModelCreating(modelBuilder);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();
        
        base.OnConfiguring(optionsBuilder);
    }
}