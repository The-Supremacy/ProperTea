using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperSagas.Ef;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProperSagasEf<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<ISagaRepository, EfSagaRepository<TContext>>();
        return services;
    }
}