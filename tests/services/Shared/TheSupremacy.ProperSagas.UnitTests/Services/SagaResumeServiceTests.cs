using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TheSupremacy.ProperSagas.Builders;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Exceptions;
using TheSupremacy.ProperSagas.Orchestration;
using TheSupremacy.ProperSagas.Services;

namespace TheSupremacy.ProperSagas.UnitTests.Services;

public class SagaResumeServiceTests
{
    private readonly Mock<ILogger<SagaResumeService>> _logger;
    private readonly SagaRegistry _registry;
    private readonly Mock<ISagaRepository> _repository;
    private readonly SagaResumeService _service;

    public SagaResumeServiceTests()
    {
        var services = new ServiceCollection();
        _repository = new Mock<ISagaRepository>();
        _registry = new SagaRegistry();
        _logger = new Mock<ILogger<SagaResumeService>>();

        // Setup services
        services.AddSingleton(_repository.Object);
        services.AddSingleton<SagaRegistry>(_registry);
        services.AddSingleton(Options.Create(new SagaOptions()));
        services.AddSingleton<ILogger<TestSagaOrchestrator>>(Mock.Of<ILogger<TestSagaOrchestrator>>());
        services.AddTransient<TestSagaOrchestrator>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        _service = new SagaResumeService(serviceProvider, _registry, _logger.Object);
    }

    #region ResumeAsync Tests

    [Fact]
    public async Task ResumeAsync_WhenSagaNotFound_ThrowsSagaNotFoundException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(sagaId)).ReturnsAsync((Saga?)null);

        // Act & Assert
        await Should.ThrowAsync<SagaNotFoundException>(async () =>
            await _service.ResumeAsync(sagaId));
    }

    [Fact]
    public async Task ResumeAsync_WhenOrchestratorNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var saga = new Saga { SagaType = "UnknownSaga", Status = SagaStatus.Running };
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _service.ResumeAsync(saga.Id));
        exception.Message.ShouldContain("No orchestrator registered");
    }

    [Fact]
    public async Task ResumeAsync_WhenSuccessful_ReturnsUpdatedSaga()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        // Act
        var result = await _service.ResumeAsync(saga.Id);

        // Assert
        result.ShouldBe(saga);
        _repository.Verify(r => r.GetByIdAsync(saga.Id), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeAsync_LogsInformation()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        // Act
        await _service.ResumeAsync(saga.Id);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Resuming saga")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ResetTimeoutAndResumeAsync Tests

    [Fact]
    public async Task ResetTimeoutAndResumeAsync_WhenSagaNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(sagaId)).ReturnsAsync((Saga?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _service.ResetTimeoutAndResumeAsync(sagaId));
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ResetTimeoutAndResumeAsync_WithNewTimeout_SetsNewTimeout()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.SetTimeout(TimeSpan.FromHours(1));
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        var newTimeout = TimeSpan.FromHours(2);

        // Act
        await _service.ResetTimeoutAndResumeAsync(saga.Id, newTimeout);

        // Assert
        saga.Timeout.ShouldBe(newTimeout);
        saga.TimeoutDeadline!.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddHours(1).AddMinutes(59));
    }

    [Fact]
    public async Task ResetTimeoutAndResumeAsync_WithoutNewTimeout_ResetsExistingTimeout()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.SetTimeout(TimeSpan.FromHours(1));
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });
        var originalDeadline = saga.TimeoutDeadline;

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        await Task.Delay(50);

        // Act
        await _service.ResetTimeoutAndResumeAsync(saga.Id);

        // Assert
        saga.Timeout.ShouldBe(TimeSpan.FromHours(1));
        saga.TimeoutDeadline!.Value.ShouldBeGreaterThan(originalDeadline!.Value);
    }

    [Fact]
    public async Task ResetTimeoutAndResumeAsync_UpdatesRepository()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.SetTimeout(TimeSpan.FromHours(1));
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        // Act
        var result = await _service.ResetTimeoutAndResumeAsync(saga.Id);

        // Assert
        result.ShouldBe(saga);
        _repository.Verify(r => r.TryUpdateAsync(saga), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResetTimeoutAndResumeAsync_LogsInformation()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.SetTimeout(TimeSpan.FromHours(1));
        saga.Steps.Add(new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending });

        _registry.Register<TestSagaOrchestrator>("TestSaga");
        _repository.Setup(r => r.GetByIdAsync(saga.Id)).ReturnsAsync(saga);
        _repository.Setup(r => r.TryUpdateAsync(It.IsAny<Saga>())).ReturnsAsync(true);

        // Act
        await _service.ResetTimeoutAndResumeAsync(saga.Id);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Resetting timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetFailedSagasAsync Tests

    [Fact]
    public async Task GetFailedSagasAsync_ReturnsFailedSagas()
    {
        // Arrange
        var failedSagas = new List<Saga>
        {
            new() { SagaType = "Saga1", Status = SagaStatus.Failed },
            new() { SagaType = "Saga2", Status = SagaStatus.Failed }
        };
        _repository.Setup(r => r.FindFailedSagasAsync()).ReturnsAsync(failedSagas);

        // Act
        var result = await _service.GetFailedSagasAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.Status == SagaStatus.Failed);
    }

    [Fact]
    public async Task GetFailedSagasAsync_WhenNoFailedSagas_ReturnsEmptyList()
    {
        // Arrange
        _repository.Setup(r => r.FindFailedSagasAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetFailedSagasAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion
}

public class TestSagaOrchestrator(
    ISagaRepository repository,
    IOptions<SagaOptions> options,
    ILogger<TestSagaOrchestrator> logger)
    : SagaOrchestratorBase(repository, options, logger)
{
    protected override string SagaType => "TestSaga";

    protected override void DefineSaga(SagaBuilder builder)
    {
        builder.AddStep("Step1",
            async ctx => await Task.CompletedTask,
            async ctx => await Task.CompletedTask);
    }
}