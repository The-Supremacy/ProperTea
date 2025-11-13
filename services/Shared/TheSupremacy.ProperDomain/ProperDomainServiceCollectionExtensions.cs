using Microsoft.Extensions.DependencyInjection;
using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain;

public static class ProperDomainServiceCollectionExtensions
{
    public static ProperDomainBuilder AddProperDomain(
        this IServiceCollection services,
        Action<ProperDomainOptions>? configure = null)
    {
        var options = new ProperDomainOptions();
        configure?.Invoke(options);
        
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.Configure(configure ?? (_ => { }));
        
        return new ProperDomainBuilder(services, options);
    }
}