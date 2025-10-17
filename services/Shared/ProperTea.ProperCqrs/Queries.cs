using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperCqrs;

public interface IQuery<TResult>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

public interface IQueryBus
{
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}


public class QueryBus : IQueryBus
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QueryBus(IServiceScopeFactory serviceScopeFactory)
    {
        this._serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return (Task<TResult>)handler.GetType()
            .GetMethod("HandleAsync")!
            .Invoke(handler, [query, ct])!;
    }
}