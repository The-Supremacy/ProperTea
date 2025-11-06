using FluentValidation;

namespace TheSupremacy.ProperCqrs;

public class ValidationQueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> handler,
    IValidator<TQuery> validator)
    : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(query, ct);
        return await handler.HandleAsync(query, ct);
    }
}