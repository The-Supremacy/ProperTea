using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProperTea.ProperIntegrationEvents.Outbox.Ef;

public static class OutboxBuilderExtensions
{
    public static OutboxBuilder AddEntityFrameworkStores<TDbContext>(this OutboxBuilder builder)
        where TDbContext : DbContext, IOutboxDbContext
    {
        builder.Services.TryAddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TDbContext>());

        builder.Services.TryAddScoped<DbContextOutboxMessagesService>();
        builder.Services.TryAddScoped<IOutboxMessagesService>(sp =>
            sp.GetRequiredService<DbContextOutboxMessagesService>());
        builder.Services.TryAddScoped<IOutboxMessagesReader>(sp =>
            sp.GetRequiredService<DbContextOutboxMessagesService>());
        builder.Services.TryAddScoped<IOutboxMessagesPublisher>(sp =>
            sp.GetRequiredService<DbContextOutboxMessagesService>());
        builder.Services.TryAddScoped<IIntegrationEventPublisher>(sp =>
            sp.GetRequiredService<DbContextOutboxMessagesService>());

        return builder;
    }
}