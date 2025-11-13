using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TheSupremacy.ProperCqrs;

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

public class QueryBus(IServiceProvider serviceProvider) : IQueryBus
{
    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);

        try
        {
            return (Task<TResult>)handler.GetType().GetMethod("HandleAsync")!.Invoke(handler, [query, ct])!;
        }
        catch (TargetInvocationException ex)
        {
            if (ex.InnerException != null)
                throw ex.InnerException;
            throw;
        }
    }
}