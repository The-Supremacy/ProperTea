using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperDdd;

public class DddBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}