using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperCqrs;

public interface IQuery;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

public interface IQueryBus
{
    Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery;
}


public class QueryBus : IQueryBus
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QueryBus(IServiceScopeFactory serviceScopeFactory)
    {
        this._serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }
}