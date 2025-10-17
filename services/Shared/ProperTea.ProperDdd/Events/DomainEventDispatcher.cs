using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperDdd.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAllAsync(CancellationToken cancellationToken = default);
    void Enqueue(IDomainEvent domainEvent);
}

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private readonly PriorityQueue<IDomainEvent, int> _events = new();

    public void Enqueue(IDomainEvent domainEvent)
    {
        var priority = (domainEvent as IPrioritizedDomainEvent)?.Priority ?? 0;
        _events.Enqueue(domainEvent, priority);
    }

    public async Task DispatchAllAsync(CancellationToken cancellationToken = default)
    {
        while (_events.TryDequeue(out var domainEvent, out _))
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