using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Services;

namespace TheSupremacy.ProperSagas.UnitTests.Services;

public class SagaBackgroundProcessorTests
{
    private readonly Mock<ILogger<SagaBackgroundProcessor>> _logger;
    private readonly SagaBackgroundProcessor _processor;
    private readonly Mock<ISagaRepository> _repository;
    private readonly Mock<ISagaResumeService> _resumeService;

    public SagaBackgroundProcessorTests()
    {
        var services = new ServiceCollection();
        _repository = new Mock<ISagaRepository>();
        var registry = new SagaRegistry();
        _logger = new Mock<ILogger<SagaBackgroundProcessor>>();
        _resumeService = new Mock<ISagaResumeService>();

        var options = Options.Create(new SagaOptions
        {
            LockTimeout = TimeSpan.FromMinutes(5),
            SagaTimeout = TimeSpan.FromMinutes(30)
        });

        services.AddSingleton(_repository.Object);
        services.AddSingleton(registry);
        services.AddSingleton(options);
        services.AddSingleton(_resumeService.Object);
        services.AddSingleton<ILogger<TestSagaOrchestrator>>(Mock.Of<ILogger<TestSagaOrchestrator>>());
        services.AddTransient<TestSagaOrchestrator>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        _processor = new SagaBackgroundProcessor(serviceProvider, options, _logger.Object);
    }

    #region ProcessScheduledSagasAsync Tests

    [Fact]
    public async Task ProcessScheduledSagasAsync_WhenNoScheduledSagas_LogsZeroCount()
    {
        // Arrange
        _repository.Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        // Act
        await _processor.ProcessScheduledSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 0 scheduled sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessScheduledSagasAsync_ProcessesReadySagas()
    {
        // Arrange
        var saga1 = new Saga { Id = Guid.NewGuid(), SagaType = "TestSaga", Status = SagaStatus.Pending };
        var saga2 = new Saga { Id = Guid.NewGuid(), SagaType = "TestSaga", Status = SagaStatus.Pending };

        _repository
            .Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([saga1, saga2]);

        _resumeService
            .Setup(s => s.ResumeAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => id == saga1.Id ? saga1 : saga2);

        // Act
        await _processor.ProcessScheduledSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 2 scheduled sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _resumeService.Verify(s => s.ResumeAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessScheduledSagasAsync_ContinuesOnError()
    {
        // Arrange
        var saga1 = new Saga { Id = Guid.NewGuid(), SagaType = "TestSaga", Status = SagaStatus.Pending };
        var saga2 = new Saga { Id = Guid.NewGuid(), SagaType = "TestSaga", Status = SagaStatus.Pending };

        _repository
            .Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([saga1, saga2]);

        _resumeService
            .SetupSequence(s => s.ResumeAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Processing failed"))
            .ReturnsAsync(saga2);

        // Act
        await _processor.ProcessScheduledSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to start/resume saga")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _resumeService.Verify(s => s.ResumeAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessScheduledSagasAsync_LogsEachProcessingAttempt()
    {
        // Arrange
        var saga = new Saga { Id = Guid.NewGuid(), SagaType = "TestSaga", Status = SagaStatus.Scheduled };

        _repository
            .Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([saga]);

        _resumeService
            .Setup(s => s.ResumeAsync(saga.Id))
            .ReturnsAsync(saga);

        // Act
        await _processor.ProcessScheduledSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Starting/resuming saga {saga.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ProcessStaleSagasAsync Tests

    [Fact]
    public async Task ProcessStaleSagasAsync_WhenNoStaleSagas_LogsZeroCount()
    {
        // Arrange
        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([]);

        // Act
        await _processor.ProcessStaleSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 0 stale sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStaleSagasAsync_ResumesAllStaleSagas()
    {
        // Arrange
        var saga1 = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        var saga2 = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        var saga3 = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };

        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([saga1, saga2, saga3]);

        _resumeService
            .Setup(s => s.ResumeAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { saga1, saga2, saga3 }.First(s => s.Id == id));

        // Act
        await _processor.ProcessStaleSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 3 stale sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _resumeService.Verify(s => s.ResumeAsync(It.IsAny<Guid>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessStaleSagasAsync_ContinuesOnIndividualFailures()
    {
        // Arrange
        var saga1 = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        var saga2 = new Saga { SagaType = "UnknownSaga", Status = SagaStatus.Running };
        var saga3 = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };

        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([saga1, saga2, saga3]);

        _resumeService
            .SetupSequence(s => s.ResumeAsync(It.IsAny<Guid>()))
            .ReturnsAsync(saga1)
            .ThrowsAsync(new Exception("Unknown saga type"))
            .ReturnsAsync(saga3);

        // Act
        await _processor.ProcessStaleSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to start/resume saga")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _resumeService.Verify(s => s.ResumeAsync(It.IsAny<Guid>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessStaleSagasAsync_UsesLockTimeoutFromOptions()
    {
        // Arrange
        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([]);

        // Act
        await _processor.ProcessStaleSagasAsync();

        // Assert
        _repository.Verify(
            r => r.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStaleSagasAsync_LogsEachResumeAttempt()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };

        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([saga]);

        _resumeService
            .Setup(s => s.ResumeAsync(saga.Id))
            .ReturnsAsync(saga);

        // Act
        await _processor.ProcessStaleSagasAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Starting/resuming saga {saga.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ProcessAllAsync Tests

    [Fact]
    public async Task ProcessAllAsync_CallsAllProcessingMethods()
    {
        // Arrange
        _repository.Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>())).ReturnsAsync([]);
        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([]);
        _repository.Setup(r => r.FindTimedOutSagasAsync()).ReturnsAsync([]);

        // Act
        await _processor.ProcessAllAsync();

        // Assert
        _repository.Verify(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()), Times.Once);
        _repository.Verify(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAllAsync_ProcessesAllSagaTypes()
    {
        // Arrange
        var scheduledSaga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Scheduled };
        var staleSaga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        var timedOutSaga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };

        _repository.Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([scheduledSaga]);
        _repository.Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([staleSaga]);
        _repository.Setup(r => r.FindTimedOutSagasAsync()).ReturnsAsync([timedOutSaga]);

        _resumeService
            .Setup(s => s.ResumeAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { scheduledSaga, staleSaga, timedOutSaga }.First(s => s.Id == id));

        // Act
        await _processor.ProcessAllAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 1 scheduled sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 1 stale sagas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _resumeService.Verify(s => s.ResumeAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessAllAsync_ProcessesScheduledSagasFirst()
    {
        // Arrange
        var callOrder = new List<string>();

        _repository
            .Setup(r => r.FindScheduledSagasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([])
            .Callback(() => callOrder.Add("scheduled"));

        _repository
            .Setup(r => r.FindSagasNeedingResumptionAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([])
            .Callback(() => callOrder.Add("stale"));

        // Act
        await _processor.ProcessAllAsync();

        // Assert
        callOrder.ShouldBe(["scheduled", "stale"]);
    }

    #endregion
}