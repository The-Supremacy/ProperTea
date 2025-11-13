using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperDomain;

public class ProperDomainBuilder(IServiceCollection services, ProperDomainOptions options)
{
    public IServiceCollection Services { get; } = services;
    
    public ProperDomainBuilder WithMaxEventIterations(int max)
    {
        options.MaxEventDispatchIterations = max;
        return this;
    }
}