using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TheSupremacy.ProperSagas.Builders;
using TheSupremacy.ProperSagas.Domain;
using TheSupremacy.ProperSagas.Exceptions;
using TheSupremacy.ProperSagas.Orchestration;

namespace TheSupremacy.ProperSagas.UnitTests.Orchestration;

public class SagaOrchestratorBaseTests
{
    private readonly Mock<ILogger<TestOrchestrator>> _loggerMock;
    private readonly IOptions<SagaOptions> _options;
    private readonly Mock<ISagaRepository> _repositoryMock;

    public SagaOrchestratorBaseTests()
    {
        _repositoryMock = new Mock<ISagaRepository>();
        _loggerMock = new Mock<ILogger<TestOrchestrator>>();
        _options = Options.Create(new SagaOptions
        {
            LockTimeout = TimeSpan.FromMinutes(5),
            SagaTimeout = TimeSpan.FromHours(1)
        });
    }

    #region Timeout Tests

    [Fact]
    public async Task ExecuteSteps_WhenTimedOut_FailsSaga()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Status = SagaStatus.Running,
            TimeoutDeadline = DateTime.UtcNow.AddMinutes(-10),
            Steps = [new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending }]
        };

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(saga.Id))
            .ReturnsAsync(saga);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var result = await orchestrator.ResumeAsync(saga.Id);

        // Assert
        result.Status.ShouldBe(SagaStatus.Failed);
        result.ErrorMessage!.ShouldContain("timed out");
    }

    #endregion

    #region Lock Management Tests

    [Fact]
    public async Task StartAsync_AcquiresLockBeforeExecution()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.LockToken.ShouldBeNull(); // Released after completion
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WithNoValidations_ExecutesAllSteps()
    {
        // Arrange
        var step1Executed = false;
        var step2Executed = false;

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ =>
                    {
                        step1Executed = true;
                        return Task.CompletedTask;
                    })
                    .AddStep("Step2", _ =>
                    {
                        step2Executed = true;
                        return Task.CompletedTask;
                    });
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Completed);
        step1Executed.ShouldBeTrue();
        step2Executed.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_WithFailingPreValidation_DoesNotExecuteSteps()
    {
        // Arrange
        var stepExecuted = false;

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddPreValidation("CheckBalance", _ =>
                        Task.FromResult(SagaValidationResult.Failure("Insufficient balance")))
                    .AddStep("ProcessPayment", _ =>
                    {
                        stepExecuted = true;
                        return Task.CompletedTask;
                    });
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.ErrorMessage!.ShouldContain("Insufficient balance");
        stepExecuted.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_WithPassingValidations_ExecutesSteps()
    {
        // Arrange
        var stepExecuted = false;

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddPreValidation("CheckInventory", _ =>
                        Task.FromResult(SagaValidationResult.Success()))
                    .AddStep("ReserveInventory", _ =>
                    {
                        stepExecuted = true;
                        return Task.CompletedTask;
                    });
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Completed);
        stepExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_WithInitialData_StoresData()
    {
        // Arrange
        var initialData = new { OrderId = Guid.NewGuid(), Amount = 100.50m };

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync(initialData);

        // Assert
        var stored = saga.GetData<object>("initialData");
        stored.ShouldNotBeNull();
    }

    [Fact]
    public async Task StartAsync_SetsCorrelationIdAndTraceId()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Step Execution Tests

    [Fact]
    public async Task ExecuteSteps_WhenStepFails_MarksStepAndSagaAsFailed()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => { builder.AddStep("FailingStep", _ => throw new Exception("Step failed")); }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.Steps[0].Status.ShouldBe(SagaStepStatus.Failed);
        saga.Steps[0].ErrorMessage!.ShouldContain("Step failed");
    }

    [Fact]
    public async Task ExecuteSteps_WhenStepFailsAfterPonr_MarksAsFailedAfterPonr()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("BeforePonr", _ => Task.CompletedTask)
                    .AddStep("Ponr", _ => Task.CompletedTask, type: SagaStepType.PointOfNoReturn)
                    .AddStep("AfterPonr", _ => throw new Exception("Critical failure"));
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.FailedAfterPonr);
        saga.ErrorMessage!.ShouldContain("AfterPonr");
    }

    [Fact]
    public async Task ExecuteSteps_SkipsCompletedSteps()
    {
        // Arrange
        var step2Executed = false;

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask)
                    .AddStep("Step2", _ =>
                    {
                        step2Executed = true;
                        return Task.CompletedTask;
                    });
            }
        };

        var existingSaga = new Saga
        {
            SagaType = "TestSaga",
            Steps =
            [
                new SagaStep { Name = "Step1", Status = SagaStepStatus.Completed },
                new SagaStep { Name = "Step2", Status = SagaStepStatus.Pending }
            ]
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(existingSaga);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        await orchestrator.ResumeAsync(existingSaga.Id);

        // Assert
        step2Executed.ShouldBeTrue();
    }

    #endregion

    #region Compensation Tests

    [Fact]
    public async Task Compensation_ExecutesInReverseOrder()
    {
        // Arrange
        var compensationOrder = new List<string>();

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask,
                        _ =>
                        {
                            compensationOrder.Add("Step1");
                            return Task.CompletedTask;
                        })
                    .AddStep("Step2", _ => Task.CompletedTask,
                        _ =>
                        {
                            compensationOrder.Add("Step2");
                            return Task.CompletedTask;
                        })
                    .AddStep("Step3", _ => throw new Exception("Failed"));
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        compensationOrder.Count.ShouldBe(2);
        compensationOrder[0].ShouldBe("Step2"); // Reverse order
        compensationOrder[1].ShouldBe("Step1");
    }

    [Fact]
    public async Task Compensation_SkipsStepsWithoutCompensation()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask,
                        _ => Task.CompletedTask)
                    .AddStep("Step2", _ => Task.CompletedTask) // No compensation
                    .AddStep("Step3", _ => throw new Exception("Failed"));
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.Steps[0].Status.ShouldBe(SagaStepStatus.Compensated);
        saga.Steps[1].Status.ShouldBe(SagaStepStatus.Completed); // Not compensated
    }

    [Fact]
    public async Task Compensation_ContinuesWhenCompensationFails()
    {
        // Arrange
        var step1Compensated = false;

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask,
                        _ =>
                        {
                            step1Compensated = true;
                            return Task.CompletedTask;
                        })
                    .AddStep("Step2", _ => Task.CompletedTask,
                        _ => throw new Exception("Compensation failed"))
                    .AddStep("Step3", _ => throw new Exception("Failed"));
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        step1Compensated.ShouldBeTrue();
        saga.Steps[1].ErrorMessage!.ShouldContain("Compensation failed");
    }

    #endregion

    #region ResumeAsync Tests

    [Fact]
    public async Task ResumeAsync_WhenSagaNotFound_ThrowsSagaNotFoundException()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Saga?)null);

        // Act & Assert
        await Should.ThrowAsync<SagaNotFoundException>(async () =>
            await orchestrator.ResumeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ResumeAsync_WhenSagaCompleted_ReturnsImmediately()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Status = SagaStatus.Completed
        };

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(saga.Id))
            .ReturnsAsync(saga);

        // Act
        var result = await orchestrator.ResumeAsync(saga.Id);

        // Assert
        result.Status.ShouldBe(SagaStatus.Completed);
        _repositoryMock.Verify(r => r.TryUpdateAsync(It.IsAny<Saga>()), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_WhenFailedToAcquireLock_ReturnsWithoutProcessing()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Status = SagaStatus.Running,
            LockToken = Guid.NewGuid(),
            LockedAt = DateTime.UtcNow
        };

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(saga.Id))
            .ReturnsAsync(saga);

        // Act
        var result = await orchestrator.ResumeAsync(saga.Id);

        // Assert
        result.Status.ShouldBe(SagaStatus.Running);
    }

    [Fact]
    public async Task ResumeAsync_ContinuesFromLastCompletedStep()
    {
        // Arrange
        var step2Executed = false;
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Status = SagaStatus.Running,
            Steps =
            [
                new SagaStep { Name = "Step1", Status = SagaStepStatus.Completed },
                new SagaStep { Name = "Step2", Status = SagaStepStatus.Pending }
            ]
        };

        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask)
                    .AddStep("Step2", _ =>
                    {
                        step2Executed = true;
                        return Task.CompletedTask;
                    });
            }
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(saga.Id))
            .ReturnsAsync(saga);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        await orchestrator.ResumeAsync(saga.Id);

        // Assert
        step2Executed.ShouldBeTrue();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteSteps_WhenCancellationRequested_CompensatesBeforePonr()
    {
        // Arrange
        var step1Compensated = false;
        var orchestrator = new TestOrchestratorWithCancellation(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", saga =>
                    {
                        saga.RequestCancellation();
                        return Task.CompletedTask;
                    }, _ =>
                    {
                        step1Compensated = true;
                        return Task.CompletedTask;
                    })
                    .AddStep("Step2", _ => Task.CompletedTask);
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        step1Compensated.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteSteps_WhenCancellationAfterPonr_ContinuesExecution()
    {
        // Arrange
        var step3Executed = false;
        var orchestrator = new TestOrchestratorWithCancellation(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddStep("Step1", _ => Task.CompletedTask)
                    .AddStep("Ponr", saga =>
                    {
                        saga.RequestCancellation();
                        return Task.CompletedTask;
                    }, type: SagaStepType.PointOfNoReturn)
                    .AddStep("Step3", _ =>
                    {
                        step3Executed = true;
                        return Task.CompletedTask;
                    });
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<Saga>()))
            .ReturnsAsync(true);

        // Act
        var saga = await orchestrator.StartAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Completed);
        step3Executed.ShouldBeTrue();
    }

    #endregion

    #region ScheduleAsync Tests

    [Fact]
    public async Task ScheduleAsync_WithNoValidations_CreatesSagaInScheduledStatus()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => { builder.AddStep("Step1", _ => Task.CompletedTask); }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Scheduled);
        saga.Steps.ShouldAllBe(s => s.Status == SagaStepStatus.Pending);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Saga>(s => s.Status == SagaStatus.Scheduled)), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_WithScheduledFor_SetsScheduledForProperty()
    {
        // Arrange
        var scheduledFor = DateTime.UtcNow.AddHours(2);
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync(scheduledFor: scheduledFor);

        // Assert
        saga.ScheduledFor.ShouldBe(scheduledFor);
    }

    [Fact]
    public async Task ScheduleAsync_WithFailingPreValidation_CreatesSagaInFailedStatus()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder
                    .AddPreValidation("CheckInventory", _ =>
                        Task.FromResult(SagaValidationResult.Failure("Out of stock")))
                    .AddStep("ProcessOrder", _ => Task.CompletedTask);
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.ErrorMessage!.ShouldContain("Out of stock");
        saga.Steps.ShouldAllBe(s => s.Status == SagaStepStatus.Pending);
    }

    [Fact]
    public async Task ScheduleAsync_WithInitialData_StoresData()
    {
        // Arrange
        var initialData = new { OrderId = Guid.NewGuid(), CustomerId = 123 };
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync(initialData);

        // Assert
        var stored = saga.GetData<object>("initialData");
        stored.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScheduleAsync_SetsCorrelationIdAndTraceId()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync();

        // Assert
        saga.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScheduleAsync_DoesNotExecuteSteps()
    {
        // Arrange
        var stepExecuted = false;
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder =>
            {
                builder.AddStep("Step1", _ =>
                {
                    stepExecuted = true;
                    return Task.CompletedTask;
                });
            }
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        await orchestrator.ScheduleAsync();

        // Assert
        stepExecuted.ShouldBeFalse();
    }

    [Fact]
    public async Task ScheduleAsync_DoesNotAcquireLock()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync();

        // Assert
        saga.LockToken.ShouldBeNull();
        saga.LockedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ScheduleAsync_WithoutScheduledFor_DoesNotSetScheduledForProperty()
    {
        // Arrange
        var orchestrator = new TestOrchestrator(_repositoryMock.Object, _options, _loggerMock.Object)
        {
            OnDefineSaga = builder => builder.AddStep("Step1", _ => Task.CompletedTask)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Saga>()))
            .Returns(Task.CompletedTask);

        // Act
        var saga = await orchestrator.ScheduleAsync();

        // Assert
        saga.ScheduledFor.ShouldBeNull();
    }

    #endregion

    #region Test Orchestrators

    public class TestOrchestrator : SagaOrchestratorBase
    {
        public TestOrchestrator(
            ISagaRepository sagaRepository,
            IOptions<SagaOptions> options,
            ILogger logger)
            : base(sagaRepository, options, logger)
        {
        }

        public Action<SagaBuilder>? OnDefineSaga { get; set; }

        protected override string SagaType => "TestSaga";

        protected override void DefineSaga(SagaBuilder builder)
        {
            OnDefineSaga?.Invoke(builder);
        }
    }

    public class TestOrchestratorWithCancellation : TestOrchestrator
    {
        public TestOrchestratorWithCancellation(
            ISagaRepository sagaRepository,
            IOptions<SagaOptions> options,
            ILogger logger)
            : base(sagaRepository, options, logger)
        {
        }

        protected override bool AllowCancellation => true;
    }

    #endregion
}