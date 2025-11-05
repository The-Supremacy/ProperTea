using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public static class OutboxBuilderExtensions
{
    public static OutboxBuilder UseEntityFrameworkStorage<TDbContext>(this OutboxBuilder builder)
        where TDbContext : DbContext, IOutboxDbContext
    {
        builder.Services.TryAddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        builder.Services.TryAddScoped<IOutboxDbContext, TDbContext>();
        builder.Services.TryAddScoped<IOutboxMessagesService, DbContextOutboxMessagesService>();

        return builder;
    }
}