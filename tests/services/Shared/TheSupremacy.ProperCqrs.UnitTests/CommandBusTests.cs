using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TheSupremacy.ProperCqrs.UnitTests;

public class CommandBusTests
{
    [Fact]
    public async Task SendAsync_SendCommand_DispatchesToCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommand("Test Data");

        // Act
        var result = await dispatcher.SendAsync(command);

        // Assert
        "Handled: Test Data".ShouldBe(result);
    }

    [Fact]
    public async Task SendAsync_SendCommandWithoutResult_DispatchesToCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommandWithoutResult("Test Data");

        // Act
        await dispatcher.SendAsync(command);

        // Assert
        "Test Data".ShouldBe(TestCommandWithoutResultHandler.HandledData);
    }
    
    [Fact]
    public async Task SendAsync_SendCommandWithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommand("");

        await Should.ThrowAsync<ValidationException>(() => dispatcher.SendAsync(command));
    }

    [Fact]
    public async Task SendAsync_SendCommandWithoutResultWithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommandWithoutResult("");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => dispatcher.SendAsync(command));
    }

    [Fact]
    public async Task SendAsync_HandlerThrowsException_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);
        services.AddScoped<ICommandHandler<ThrowingCommand, string>, ThrowingCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(new ThrowingCommand()));
    }

    [Fact]
    public async Task SendAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<TaskCanceledException>(() =>
            dispatcher.SendAsync(new LongRunningCommand(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_CommandWithoutValidator_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        var command = new CommandWithoutValidator("test");
        var result = await dispatcher.SendAsync(command);

        "Handled: test".ShouldBe(result);
    }
}

public record TestCommand(string Data) : ICommand<string>;

public record TestCommandWithoutResult(string Data) : ICommand;

public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<string> HandleAsync(TestCommand command, CancellationToken ct = default)
    {
        return Task.FromResult($"Handled: {command.Data}");
    }
}

public class TestCommandWithoutResultHandler : ICommandHandler<TestCommandWithoutResult>
{
    public static string? HandledData;

    public Task HandleAsync(TestCommandWithoutResult command, CancellationToken ct = default)
    {
        HandledData = command.Data;
        return Task.CompletedTask;
    }
}

public class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class TestCommandWithoutResultValidator : AbstractValidator<TestCommandWithoutResult>
{
    public TestCommandWithoutResultValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
    }
}

public record ThrowingCommand : ICommand<string>;

public class ThrowingCommandHandler : ICommandHandler<ThrowingCommand, string>
{
    public Task<string> HandleAsync(ThrowingCommand command, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Handler intentionally threw an exception");
    }
}

public record LongRunningCommand : ICommand<string>;

public class LongRunningCommandHandler : ICommandHandler<LongRunningCommand, string>
{
    public async Task<string> HandleAsync(LongRunningCommand command, CancellationToken ct = default)
    {
        await Task.Delay(5000, ct); // Will throw if cancelled
        return "Completed";
    }
}

public record CommandWithoutValidator(string Data) : ICommand<string>;

public class CommandWithoutValidatorHandler : ICommandHandler<CommandWithoutValidator, string>
{
    public Task<string> HandleAsync(CommandWithoutValidator command, CancellationToken ct = default)
    {
        return Task.FromResult($"Handled: {command.Data}");
    }
}