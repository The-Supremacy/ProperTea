using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperSagas.Ef;

/// <summary>
/// Extension methods for registering ProperSagas EF Core services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ProperSagas with Entity Framework Core persistence
    /// </summary>
    /// <typeparam name="TContext">Your DbContext type that has a DbSet&lt;SagaEntity&gt; Sagas property</typeparam>
    public static IServiceCollection AddProperSagasEf<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<ISagaRepository, EfSagaRepository<TContext>>();
        return services;
    }
}

