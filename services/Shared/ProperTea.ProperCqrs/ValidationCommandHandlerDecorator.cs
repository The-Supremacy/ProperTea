using FluentValidation;

namespace ProperTea.ProperCqrs;

public class ValidationCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _handler;
    private readonly IValidator<TCommand> _validator;

    public ValidationCommandHandlerDecorator(ICommandHandler<TCommand, TResult> handler, IValidator<TCommand> validator)
    {
        _handler = handler;
        _validator = validator;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken: ct);
        return await _handler.HandleAsync(command, ct);
    }
}

public class ValidationCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _handler;
    private readonly IValidator<TCommand> _validator;

    public ValidationCommandHandlerDecorator(ICommandHandler<TCommand> handler, IValidator<TCommand> validator)
    {
        _handler = handler;
        _validator = validator;
    }

    public async Task HandleAsync(TCommand command, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken: ct);
        await _handler.HandleAsync(command, ct);
    }
}