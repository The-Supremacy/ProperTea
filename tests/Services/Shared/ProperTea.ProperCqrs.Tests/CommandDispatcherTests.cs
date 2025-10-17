using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperCqrs;

namespace ProperTea.ProperCqrs.Tests;

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

public class CommandDispatcherTests
{
    [Fact]
    public async Task SendCommand_Should_DispatchToCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandDispatcherTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommand("Test Data");

        // Act
        var result = await dispatcher.SendAsync(command);

        // Assert
        Assert.Equal("Handled: Test Data", result);
    }

    [Fact]
    public async Task SendCommand_WithInvalidData_Should_ThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandDispatcherTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();
        
        var command = new TestCommand("");
        
        await Assert.ThrowsAsync<ValidationException>(() => dispatcher.SendAsync(command));
    }
    
    [Fact]
    public async Task SendCommandWithoutResult_Should_DispatchToCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandDispatcherTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommandWithoutResult("Test Data");

        // Act
        await dispatcher.SendAsync(command);

        // Assert
        Assert.Equal("Test Data", TestCommandWithoutResultHandler.HandledData);
    }

    [Fact]
    public async Task SendCommandWithoutResult_WithInvalidData_Should_ThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(CommandDispatcherTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<ICommandBus>();

        var command = new TestCommandWithoutResult("");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => dispatcher.SendAsync(command));
    }
}
