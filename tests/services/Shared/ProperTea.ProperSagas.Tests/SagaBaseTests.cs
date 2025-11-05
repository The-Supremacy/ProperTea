using Shouldly;

namespace ProperTea.ProperSagas.Tests;

public class SagaBaseTests
{
    // AllPreValidationStepsCompleted tests
    [Fact]
    public void AllPreValidationStepsCompleted_AllStepsCompleted_ReturnsTrue()
    {
        // Arrange
        var saga = new TestSaga();
        saga.Steps[0].Status = SagaStepStatus.Completed;
        saga.Steps[1].Status = SagaStepStatus.Completed;

        // Act
        var allCompleted = saga.AllPreValidationStepsCompleted();

        // Assert
        allCompleted.ShouldBeTrue();
    }

    [Fact]
    public void AllPreValidationStepsCompleted_SomeStepsNotCompleted_ReturnsFalse()
    {
        // Arrange
        var saga = new TestSaga();
        saga.Steps[0].Status = SagaStepStatus.Completed;
        saga.Steps[1].Status = SagaStepStatus.Pending;

        // Act
        var allCompleted = saga.AllPreValidationStepsCompleted();

        // Assert
        allCompleted.ShouldBeFalse();
    }

    // GetData tests
    [Fact]
    public void GetData_NonExistentKey_ReturnsDefault()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        var result = saga.GetData<string>("nonexistent");

        // Assert
        result.ShouldBeNull();
    }

    // GetExecutionSteps tests
    [Fact]
    public void GetExecutionSteps_ReturnsOnlyExecutionSteps()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        var executionSteps = saga.GetExecutionSteps().ToList();

        // Assert
        executionSteps.Count.ShouldBe(3);
        executionSteps.ShouldAllBe(step => !step.IsPreValidation);
        executionSteps.ShouldContain(s => s.Name == "Execute1");
        executionSteps.ShouldContain(s => s.Name == "Execute2");
        executionSteps.ShouldContain(s => s.Name == "Execute3");
    }

    // GetPreValidationSteps tests
    [Fact]
    public void GetPreValidationSteps_ReturnsOnlyPreValidationSteps()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        var validationSteps = saga.GetPreValidationSteps().ToList();

        // Assert
        validationSteps.Count.ShouldBe(2);
        validationSteps.ShouldAllBe(step => step.IsPreValidation);
        validationSteps.ShouldContain(s => s.Name == "Validate1");
        validationSteps.ShouldContain(s => s.Name == "Validate2");
    }

    // GetStepsNeedingCompensation tests
    [Fact]
    public void GetStepsNeedingCompensation_CompletedStepsWithCompensation_ReturnsInReverseOrder()
    {
        // Arrange
        var saga = new TestSaga();
        saga.Steps[2].Status = SagaStepStatus.Completed; // Execute1 - has compensation
        saga.Steps[3].Status = SagaStepStatus.Completed; // Execute2 - has compensation
        saga.Steps[4].Status = SagaStepStatus.Completed; // Execute3 - no compensation

        // Act
        var stepsNeedingCompensation = saga.GetStepsNeedingCompensation().ToList();

        // Assert
        stepsNeedingCompensation.Count.ShouldBe(2);
        stepsNeedingCompensation[0].Name.ShouldBe("Execute2"); // Reverse order
        stepsNeedingCompensation[1].Name.ShouldBe("Execute1");
    }

    // HasData tests
    [Fact]
    public void HasData_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var saga = new TestSaga();
        saga.SetData("key", "value");

        // Act & Assert
        saga.HasData("key").ShouldBeTrue();
    }

    [Fact]
    public void HasData_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var saga = new TestSaga();

        // Act & Assert
        saga.HasData("nonexistent").ShouldBeFalse();
    }

    // MarkAsCompleted tests
    [Fact]
    public void MarkAsCompleted_SetsStatusAndCompletedAt()
    {
        // Arrange
        var saga = new TestSaga();
        var beforeComplete = DateTime.UtcNow;

        // Act
        saga.MarkAsCompleted();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Completed);
        saga.CompletedAt.ShouldNotBeNull();
        saga.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeComplete);
    }

    // MarkAsFailed tests
    [Fact]
    public void MarkAsFailed_SetsStatusAndErrorMessage()
    {
        // Arrange
        var saga = new TestSaga();
        var errorMessage = "Test error";

        // Act
        saga.MarkAsFailed(errorMessage);

        // Assert
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.ErrorMessage.ShouldBe(errorMessage);
        saga.CompletedAt.ShouldNotBeNull();
    }

    // MarkAsRunning tests
    [Fact]
    public void MarkAsRunning_SetsStatus()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        saga.MarkAsRunning();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Running);
    }

    // MarkAsWaitingForCallback tests
    [Fact]
    public void MarkAsWaitingForCallback_SetsStatusAndData()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        saga.MarkAsWaitingForCallback("user_approval");

        // Assert
        saga.Status.ShouldBe(SagaStatus.WaitingForCallback);
        saga.GetData<string>("waitingFor").ShouldBe("user_approval");
    }

    // MarkStepAsCompleted tests
    [Fact]
    public void MarkStepAsCompleted_UpdatesStepStatus()
    {
        // Arrange
        var saga = new TestSaga();
        var stepName = "Execute1";

        // Act
        saga.MarkStepAsCompleted(stepName);

        // Assert
        var step = saga.Steps.First(s => s.Name == stepName);
        step.Status.ShouldBe(SagaStepStatus.Completed);
        step.CompletedAt.ShouldNotBeNull();
    }

    // MarkStepAsFailed tests
    [Fact]
    public void MarkStepAsFailed_UpdatesStatusAndError()
    {
        // Arrange
        var saga = new TestSaga();
        var stepName = "Execute1";
        var errorMessage = "Step failed";

        // Act
        saga.MarkStepAsFailed(stepName, errorMessage);

        // Assert
        var step = saga.Steps.First(s => s.Name == stepName);
        step.Status.ShouldBe(SagaStepStatus.Failed);
        step.ErrorMessage.ShouldBe(errorMessage);
        step.CompletedAt.ShouldNotBeNull();
    }

    // SetData tests
    [Fact]
    public void SetData_PrimitiveTypes_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var saga = new TestSaga();
        var userId = Guid.NewGuid();

        // Act
        saga.SetData("userId", userId);
        saga.SetData("email", "test@example.com");
        saga.SetData("count", 42);
        saga.SetData("amount", 100.50m);
        saga.SetData("isActive", true);

        // Assert
        saga.GetData<Guid>("userId").ShouldBe(userId);
        saga.GetData<string>("email").ShouldBe("test@example.com");
        saga.GetData<int>("count").ShouldBe(42);
        saga.GetData<decimal>("amount").ShouldBe(100.50m);
        saga.GetData<bool>("isActive").ShouldBeTrue();
    }

    private class TestSaga : SagaBase
    {
        public TestSaga()
        {
            Steps = new List<SagaStep>
            {
                new() { Name = "Validate1", IsPreValidation = true, HasCompensation = false },
                new() { Name = "Validate2", IsPreValidation = true, HasCompensation = false },
                new() { Name = "Execute1", HasCompensation = true },
                new() { Name = "Execute2", HasCompensation = true },
                new() { Name = "Execute3", HasCompensation = false }
            };
        }
    }
}