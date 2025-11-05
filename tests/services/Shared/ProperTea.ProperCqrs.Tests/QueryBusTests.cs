using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.ProperCqrs.Tests;

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

public class QueryBusTests
{
    [Fact]
    public async Task SendAsync_SendQuery_DispatchesToCorrectHandler()
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
        Assert.Equal("Handled: Test Data", result);
    }
    
    [Fact]
    public async Task SendAsync_SendQueryWithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProperCqrs(typeof(QueryBusTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IQueryBus>();

        var query = new TestQuery("");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => dispatcher.SendAsync(query));
    }
}