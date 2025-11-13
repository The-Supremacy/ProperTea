using Microsoft.EntityFrameworkCore;

namespace TheSupremacy.ProperDomain.Persistence.Ef.IntegrationTests.Setup;

public class TestDbContext : DbContext
{
    public TestDbContext()
    {
    }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestAggregate> TestAggregates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestAggregate>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name);
            builder.Ignore(x => x.DomainEvents);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();

        base.OnConfiguring(optionsBuilder);
    }
}