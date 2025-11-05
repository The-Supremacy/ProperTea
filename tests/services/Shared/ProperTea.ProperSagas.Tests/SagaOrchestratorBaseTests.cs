using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ProperTea.ProperSagas.Tests;

public class SagaOrchestratorBaseTests
{
    public class TestSaga : SagaBase
    {
        public TestSaga()
        {
            Steps = new List<SagaStep>
            {
                new() { Name = "Validate", IsPreValidation = true, HasCompensation = false },
                new() { Name = "Execute", HasCompensation = true },
                new() { Name = "Finalize", HasCompensation = false }
            };
        }
    }

    public class TestOrchestrator : SagaOrchestratorBase<TestSaga>
    {
        public bool ExecuteStepsCalled { get; private set; }
        public bool CompensateCalled { get; private set; }
        public bool ValidateStepCalled { get; private set; }
        public string? LastValidatedStepName { get; private set; }

        public TestOrchestrator(ISagaRepository repository, ILogger<TestOrchestrator> logger)
            : base(repository, logger)
        {
        }

        public override Task<TestSaga> StartAsync(TestSaga saga)
        {
            saga.MarkAsRunning();
            ExecuteStepsCalled = true;
            return Task.FromResult(saga);
        }

        protected override Task ExecuteStepsAsync(TestSaga saga)
        {
            ExecuteStepsCalled = true;
            return Task.CompletedTask;
        }

        protected override Task CompensateAsync(TestSaga saga)
        {
            CompensateCalled = true;
            return Task.CompletedTask;
        }

        protected override Task ValidateStepAsync(TestSaga saga, string stepName)
        {
            ValidateStepCalled = true;
            LastValidatedStepName = stepName;
            return Task.CompletedTask;
        }

        // Expose protected method for testing
        public Task<bool> TestExecuteStepAsync(TestSaga saga, string stepName, Func<Task> action)
        {
            return ExecuteStepAsync(saga, stepName, action);
        }

        // Expose protected method for testing
        public Task TestCompensateCompletedAsync(TestSaga saga, Func<TestSaga, string, Task> compensationAction)
        {
            return CompensateCompletedAsync(saga, compensationAction);
        }
    }

    // CompensateCompletedAsync tests
    [Fact]
    public async Task CompensateCompletedAsync_StepsWithCompensation_CompensatesOnlyThoseSteps()
    {
        // Arrange
        var saga = new TestSaga();
        saga.Steps[1].Status = SagaStepStatus.Completed; // Execute - has compensation
        saga.Steps[2].Status = SagaStepStatus.Completed; // Finalize - no compensation

        var mockRepository = new Mock<ISagaRepository>();
        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);

        var compensatedSteps = new List<string>();

        // Act
        await orchestrator.TestCompensateCompletedAsync(saga, async (s, stepName) =>
        {
            compensatedSteps.Add(stepName);
            await Task.CompletedTask;
        });

        // Assert
        compensatedSteps.Count.ShouldBe(1);
        compensatedSteps[0].ShouldBe("Execute");
        saga.Status.ShouldBe(SagaStatus.Compensated);
    }

    [Fact]
    public async Task CompensateCompletedAsync_CompensationFails_ContinuesWithOtherSteps()
    {
        // Arrange
        var saga = new TestSaga();
        saga.Steps[1].Status = SagaStepStatus.Completed; // Execute
        saga.Steps[1].HasCompensation = true;
        saga.Steps[2].Status = SagaStepStatus.Completed; // Finalize
        saga.Steps[2].HasCompensation = true; // Override to allow compensation

        var mockRepository = new Mock<ISagaRepository>();
        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);

        var compensatedSteps = new List<string>();

        // Act
        await orchestrator.TestCompensateCompletedAsync(saga, async (s, stepName) =>
        {
            compensatedSteps.Add(stepName);
            if (stepName == "Finalize")
            {
                throw new InvalidOperationException("Compensation failed");
            }
            await Task.CompletedTask;
        });

        // Assert
        compensatedSteps.Count.ShouldBe(2); // Should continue despite failure (reverse order: Finalize then Execute)
        compensatedSteps[0].ShouldBe("Finalize"); // Compensated in reverse order
        compensatedSteps[1].ShouldBe("Execute");
        saga.Status.ShouldBe(SagaStatus.Compensated);
    }

    // ExecuteStepAsync tests
    [Fact]
    public async Task ExecuteStepAsync_StepSucceeds_MarksStepAsCompleted()
    {
        // Arrange
        var mockRepository = new Mock<ISagaRepository>();
        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);
        var saga = new TestSaga();

        // Act
        var result = await orchestrator.TestExecuteStepAsync(saga, "Execute", async () =>
        {
            await Task.CompletedTask;
        });

        // Assert
        result.ShouldBeTrue();
        var step = saga.Steps.First(s => s.Name == "Execute");
        step.Status.ShouldBe(SagaStepStatus.Completed);
        step.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteStepAsync_StepThrowsException_MarksStepAsFailed()
    {
        // Arrange
        var mockRepository = new Mock<ISagaRepository>();
        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);
        var saga = new TestSaga();
        var errorMessage = "Test error";

        // Act
        var result = await orchestrator.TestExecuteStepAsync(saga, "Execute", async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException(errorMessage);
        });

        // Assert
        result.ShouldBeFalse();
        var step = saga.Steps.First(s => s.Name == "Execute");
        step.Status.ShouldBe(SagaStepStatus.Failed);
        step.ErrorMessage.ShouldBe(errorMessage);
    }

    // ResumeAsync tests
    [Fact]
    public async Task ResumeAsync_SagaNotCompleted_ContinuesExecution()
    {
        // Arrange
        var saga = new TestSaga();
        saga.MarkAsRunning();

        var mockRepository = new Mock<ISagaRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync<TestSaga>(saga.Id))
            .ReturnsAsync(saga);

        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await orchestrator.ResumeAsync(saga.Id);

        // Assert
        result.ShouldNotBeNull();
        orchestrator.ExecuteStepsCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ResumeAsync_SagaCompleted_DoesNotResumeExecution()
    {
        // Arrange
        var saga = new TestSaga();
        saga.MarkAsCompleted();

        var mockRepository = new Mock<ISagaRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync<TestSaga>(saga.Id))
            .ReturnsAsync(saga);

        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);

        // Act
        var result = await orchestrator.ResumeAsync(saga.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(SagaStatus.Completed);
    }

    // ValidateAsync tests
    [Fact]
    public async Task ValidateAsync_RunsOnlyPreValidationSteps()
    {
        // Arrange
        var mockRepository = new Mock<ISagaRepository>();
        var mockLogger = new Mock<ILogger<TestOrchestrator>>();
        var orchestrator = new TestOrchestrator(mockRepository.Object, mockLogger.Object);
        var saga = new TestSaga();

        // Act
        var (isValid, errorMessage) = await orchestrator.ValidateAsync(saga);

        // Assert
        isValid.ShouldBeTrue();
        errorMessage.ShouldBeNull();
        orchestrator.ValidateStepCalled.ShouldBeTrue();
        orchestrator.LastValidatedStepName.ShouldBe("Validate");
    }
}
