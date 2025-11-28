using Microsoft.EntityFrameworkCore;

namespace ProperTea.Organization.Core.Persistence;

public class OrganizationDbContext : DbContext
{
    public OrganizationDbContext()
    {
    }

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
}
