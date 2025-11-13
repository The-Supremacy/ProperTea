using Shouldly;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.UnitTests.Domain;

public class SagaStepTests
{
    #region CanCompensate Tests

    [Fact]
    public void CanCompensate_WhenExecutionStepWithCompensationName_ReturnsTrue()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "CreateOrder",
            Type = SagaStepType.Execution,
            CompensationName = "CancelOrder"
        };

        // Act
        var result = step.CanCompensate;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanCompensate_WhenExecutionStepWithoutCompensationName_ReturnsFalse()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "LogEvent",
            Type = SagaStepType.Execution,
            CompensationName = null
        };

        // Act
        var result = step.CanCompensate;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanCompensate_WhenExecutionStepWithEmptyCompensationName_ReturnsFalse()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "LogEvent",
            Type = SagaStepType.Execution,
            CompensationName = string.Empty
        };

        // Act
        var result = step.CanCompensate;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanCompensate_WhenNoCompensationStep_ReturnsFalse()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "LogEvent",
            Type = SagaStepType.NoCompensation,
            CompensationName = "SomeCompensation" // Even with compensation name
        };

        // Act
        var result = step.CanCompensate;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanCompensate_WhenPointOfNoReturnStep_ReturnsFalse()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "CommitTransaction",
            Type = SagaStepType.PointOfNoReturn,
            CompensationName = "RollbackTransaction" // Even with compensation name
        };

        // Act
        var result = step.CanCompensate;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var step = new SagaStep();

        // Assert
        step.Name.ShouldBe(string.Empty);
        step.Status.ShouldBe(SagaStepStatus.Pending);
        step.Type.ShouldBe(SagaStepType.Execution);
        step.ErrorMessage.ShouldBeNull();
        step.StartedAt.ShouldBeNull();
        step.CompletedAt.ShouldBeNull();
        step.CompensationName.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var step = new SagaStep
        {
            Name = "TestStep",
            Status = SagaStepStatus.Running,
            Type = SagaStepType.Execution,
            ErrorMessage = "Test error",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(5),
            CompensationName = "TestCompensation"
        };

        // Assert
        step.Name.ShouldBe("TestStep");
        step.Status.ShouldBe(SagaStepStatus.Running);
        step.Type.ShouldBe(SagaStepType.Execution);
        step.ErrorMessage.ShouldBe("Test error");
        step.StartedAt.ShouldNotBeNull();
        step.CompletedAt.ShouldNotBeNull();
        step.CompensationName.ShouldBe("TestCompensation");
    }

    #endregion

    #region Status Enum Tests

    [Fact]
    public void SagaStepStatus_HasExpectedValues()
    {
        // Assert
        ((int)SagaStepStatus.Pending).ShouldBe(0);
        ((int)SagaStepStatus.Running).ShouldBe(1);
        ((int)SagaStepStatus.Completed).ShouldBe(2);
        ((int)SagaStepStatus.Failed).ShouldBe(3);
        ((int)SagaStepStatus.Compensated).ShouldBe(4);
    }

    [Fact]
    public void SagaStepType_HasExpectedValues()
    {
        // Assert
        ((int)SagaStepType.Execution).ShouldBe(0);
        ((int)SagaStepType.NoCompensation).ShouldBe(1);
        ((int)SagaStepType.PointOfNoReturn).ShouldBe(2);
    }

    #endregion
}