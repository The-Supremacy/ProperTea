using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProperTea.ProperIntegrationEvents;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddProperOutbox<TDbContext>(
        this IServiceCollection services,
        Action<IIntegrationEventPublisherBuilder> buildAction)
        where TDbContext : DbContext, IOutboxDbContext
    {
        services.TryAddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        services.TryAddScoped<IOutboxDbContext, TDbContext>();
        services.TryAddScoped<IOutboxMessagesService, DbContextOutboxMessagesService>();
        
        var builder = new IntegrationEventPublisherBuilder(services);
        buildAction(builder);

        return services;
    }
}

public interface IIntegrationEventPublisherBuilder
{
    IServiceCollection Services { get; }
}

internal class IntegrationEventPublisherBuilder(IServiceCollection services) : IIntegrationEventPublisherBuilder
{
    public IServiceCollection Services => services;
}