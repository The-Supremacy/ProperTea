using Microsoft.EntityFrameworkCore;

namespace ProperTea.Organization.Persistence;

public class OrganizationDbContext : DbContext
{
    public OrganizationDbContext()
    {
    }

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Organization> Organizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
}
