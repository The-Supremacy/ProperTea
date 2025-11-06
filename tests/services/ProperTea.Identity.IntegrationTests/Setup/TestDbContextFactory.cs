using Microsoft.EntityFrameworkCore;
using ProperTea.Identity.Kernel.Data;

namespace ProperTea.Identity.IntegrationTests.Setup;

public static class TestDbContextFactory
{
    public static ProperTeaIdentityDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ProperTeaIdentityDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new ProperTeaIdentityDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}