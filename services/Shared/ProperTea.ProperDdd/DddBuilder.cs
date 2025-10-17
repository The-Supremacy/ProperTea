using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperDdd.Events;

namespace ProperTea.ProperDdd;

public class DddBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}