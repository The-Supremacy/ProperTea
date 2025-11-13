using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TheSupremacy.ProperCqrs.UnitTests;

public class QueryBusTests
{
    [Fact]
    public async Task SendAsync_SendQuery_DispatchToCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IQueryBus>();

        var query = new TestQuery("Test Data");

        // Act
        var result = await dispatcher.SendAsync(query);

        // Assert
        "Handled: Test Data".ShouldBe(result);
    }

    [Fact]
    public async Task SendAsync_SendQueryWithInvalidData_ThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IQueryBus>();

        var query = new TestQuery("");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => dispatcher.SendAsync(query));
    }

    [Fact]
    public async Task SendAsync_HandlerNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        // Don't register any handlers from this assembly
        services.AddScoped<IQueryBus, QueryBus>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryBus>();

        var command = new TestQuery("data");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(command));
    }
    
    [Fact]
    public async Task SendAsync_HandlerThrowsException_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);
        services.AddScoped<IQueryHandler<ThrowingQuery, string>, ThrowingQueryHandler>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryBus>();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(new ThrowingQuery()));
    }
    
    
    [Fact]
    public async Task SendAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryBus>();

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<TaskCanceledException>(() =>
            dispatcher.SendAsync(new LongRunningQuery(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_CommandWithoutValidator_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryBus>();

        var command = new QueryWithoutValidator("test");
        var result = await dispatcher.SendAsync(command);

        "Handled: test".ShouldBe(result);
    }
}

public record TestQuery(string Data) : IQuery<string>;

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery query, CancellationToken ct = default)
    {
        return Task.FromResult($"Handled: {query.Data}");
    }
}

public class TestQueryValidator : AbstractValidator<TestQuery>
{
    public TestQueryValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
    }
}

public record ThrowingQuery : IQuery<string>;

public class ThrowingQueryHandler : IQueryHandler<ThrowingQuery, string>
{
    public Task<string> HandleAsync(ThrowingQuery query, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Handler intentionally threw an exception");
    }
}

public record LongRunningQuery : IQuery<string>;

public class LongRunningQueryHandler : IQueryHandler<LongRunningQuery, string>
{
    public async Task<string> HandleAsync(LongRunningQuery query, CancellationToken ct = default)
    {
        await Task.Delay(5000, ct); // Will throw if cancelled
        return "Completed";
    }
}

public record QueryWithoutValidator(string Data) : IQuery<string>;

public class QueryWithoutValidatorHandler : IQueryHandler<QueryWithoutValidator, string>
{
    public Task<string> HandleAsync(QueryWithoutValidator query, CancellationToken ct = default)
    {
        return Task.FromResult($"Handled: {query.Data}");
    }
}