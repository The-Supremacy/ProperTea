using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperDdd.Persistence.Ef;

public static class DddBuilderExtensions
{
    public static DddBuilder UseEntityFramework<TDbContext>(this DddBuilder builder)
        where TDbContext : DbContext
    {
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
        builder.Services.TryAddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        return builder;
    }
}