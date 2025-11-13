using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperDomain.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAllAsync(CancellationToken cancellationToken = default);
    void Enqueue(IDomainEvent domainEvent);
}

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private readonly Queue<IDomainEvent> _events = new();

    public void Enqueue(IDomainEvent domainEvent)
    {
        _events.Enqueue(domainEvent);
    }

    public async Task DispatchAllAsync(CancellationToken cancellationToken = default)
    {
        while (_events.TryDequeue(out var domainEvent))
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
                await (Task)handlerType
                    .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                    .Invoke(handler, [domainEvent, cancellationToken])!;
        }
    }
}