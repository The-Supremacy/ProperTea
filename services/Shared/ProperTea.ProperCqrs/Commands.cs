using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperCqrs;

public interface ICommand;

public interface ICommand<TResult> : ICommand;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface ICommandBus
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);

    Task SendAsync(ICommand command, CancellationToken ct = default);
}

public class CommandBus : ICommandBus
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CommandBus(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return (Task<TResult>)handler.GetType()
            .GetMethod("HandleAsync")!
            .Invoke(handler, [command, ct])!;
    }

    public Task SendAsync(ICommand command, CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return (Task)handler.GetType()
            .GetMethod("HandleAsync")!
            .Invoke(handler, new object[] { command, ct })!;
    }
}