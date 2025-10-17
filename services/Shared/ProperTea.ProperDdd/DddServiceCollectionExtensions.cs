using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperDdd.Events;

namespace ProperTea.ProperDdd;

public static class DddServiceCollectionExtensions
{
    public static DddBuilder AddProperDdd(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return new DddBuilder(services);
    }
}