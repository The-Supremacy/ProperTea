using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TheSupremacy.ProperDomain.Persistence.Ef;

public static class ProperDomainBuilderExtensions
{
    public static ProperDomainBuilder UseEntityFramework<TDbContext>(this ProperDomainBuilder builder)
        where TDbContext : DbContext
    {
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
        builder.Services.TryAddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        return builder;
    }
}