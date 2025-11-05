using FluentValidation;

namespace ProperTea.ProperCqrs;

public class ValidationQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _handler;
    private readonly IValidator<TQuery> _validator;

    public ValidationQueryHandlerDecorator(IQueryHandler<TQuery, TResult> handler, IValidator<TQuery> validator)
    {
        _handler = handler;
        _validator = validator;
    }

    public async Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(query, ct);
        return await _handler.HandleAsync(query, ct);
    }
}