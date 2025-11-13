using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProperSagasEf<TContext>(this IServiceCollection services)
        where TContext : DbContext, ISagaDbContext
    {
        services.AddScoped<ISagaRepository, EfSagaRepository<TContext>>();
        return services;
    }
}