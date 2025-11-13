using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperIntegrationEvents.Outbox;

public class OutboxBuilder
{
    public OutboxBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}