using FluentValidation;

namespace TheSupremacy.ProperCqrs;

public class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> handler,
    IValidator<TCommand> validator)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        return await handler.HandleAsync(command, ct);
    }
}

public class ValidationCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> handler,
    IValidator<TCommand> validator)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task HandleAsync(TCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        await handler.HandleAsync(command, ct);
    }
}