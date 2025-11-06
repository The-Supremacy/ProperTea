using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProperTea.Identity.Kernel.Models;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;

namespace ProperTea.Identity.Kernel.Data;

public class ProperTeaIdentityDbContext : IdentityDbContext<ProperTeaUser, IdentityRole<Guid>, Guid>, IOutboxDbContext
{
    public ProperTeaIdentityDbContext(DbContextOptions<ProperTeaIdentityDbContext> options) : base(options)
    {
    }

    public ProperTeaIdentityDbContext()
    {
    }

    public DbSet<SecurityLog> SecurityLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}