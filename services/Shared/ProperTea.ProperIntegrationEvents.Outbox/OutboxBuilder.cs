using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperIntegrationEvents.Outbox;

public class OutboxBuilder
{
    public IServiceCollection Services { get; }

    public OutboxBuilder(IServiceCollection services)
    {
        Services = services;
    }
}