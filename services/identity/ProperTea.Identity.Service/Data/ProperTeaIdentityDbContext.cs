using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Data;

public class ProperTeaIdentityDbContext : IdentityDbContext<ProperTeaUser, IdentityRole<Guid>, Guid>
{
    public ProperTeaIdentityDbContext(DbContextOptions<ProperTeaIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<SecurityLog> SecurityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // This will automatically scan the assembly for all IEntityTypeConfiguration classes
        // and apply them. This keeps the DbContext clean.
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}