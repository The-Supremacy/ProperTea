using Polly.Retry;
using Shouldly;
using TheSupremacy.ProperSagas.Builders;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.UnitTests.Builders;

public class SagaBuilderTests
{
    #region Integration Tests

    [Fact]
    public void FluentAPI_ComplexSaga_BuildsCorrectly()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder
            .AddPreValidation("CheckInventory", saga => Task.FromResult(SagaValidationResult.Success()))
            .AddPreValidation("ValidatePayment", saga => Task.FromResult(SagaValidationResult.Success()))
            .AddStep("ReserveInventory", saga => Task.CompletedTask, saga => Task.CompletedTask)
            .AddStep("ChargePayment", saga => Task.CompletedTask, saga => Task.CompletedTask)
            .AddStep("CommitOrder", saga => Task.CompletedTask, type: SagaStepType.PointOfNoReturn)
            .AddStep("SendConfirmation", saga => Task.CompletedTask, type: SagaStepType.NoCompensation);

        var saga = builder.BuildSaga("OrderSaga");
        var validations = builder.BuildValidationDefinitions();
        var definitions = builder.BuildStepDefinitions();

        // Assert
        validations.Count.ShouldBe(2);
        saga.Steps.Count.ShouldBe(4);
        definitions.Count.ShouldBe(4);

        saga.Steps[0].Name.ShouldBe("ReserveInventory");
        saga.Steps[0].CompensationName.ShouldNotBeNull();

        saga.Steps[2].Name.ShouldBe("CommitOrder");
        saga.Steps[2].Type.ShouldBe(SagaStepType.PointOfNoReturn);

        saga.Steps[3].Name.ShouldBe("SendConfirmation");
        saga.Steps[3].Type.ShouldBe(SagaStepType.NoCompensation);
    }

    #endregion

    #region AddStep Tests

    [Fact]
    public void AddStep_AddsStepToBuilder()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStep("TestStep", saga => Task.CompletedTask);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps.Count.ShouldBe(1);
        saga.Steps[0].Name.ShouldBe("TestStep");
        saga.Steps[0].Type.ShouldBe(SagaStepType.Execution);
        saga.Steps[0].Status.ShouldBe(SagaStepStatus.Pending);
    }

    [Fact]
    public void AddStep_WithCompensation_SetsCompensationName()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStep(
            "CreateOrder",
            saga => Task.CompletedTask,
            saga => Task.CompletedTask);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps[0].CompensationName.ShouldBe("CreateOrder");
    }

    [Fact]
    public void AddStep_WithoutCompensation_LeavesCompensationNameNull()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStep("LogEvent", saga => Task.CompletedTask);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps[0].CompensationName.ShouldBeNull();
    }

    [Fact]
    public void AddStep_WithPointOfNoReturn_SetsCorrectType()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStep(
            "CommitTransaction",
            saga => Task.CompletedTask,
            type: SagaStepType.PointOfNoReturn);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps[0].Type.ShouldBe(SagaStepType.PointOfNoReturn);
    }

    [Fact]
    public void AddStep_WithNoCompensationType_SetsCorrectType()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStep(
            "LogEvent",
            saga => Task.CompletedTask,
            type: SagaStepType.NoCompensation);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps[0].Type.ShouldBe(SagaStepType.NoCompensation);
    }

    [Fact]
    public void AddStep_WithRetryOptions_CreatesResiliencePipeline()
    {
        // Arrange
        var builder = new SagaBuilder();
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        };

        // Act
        builder.AddStep("RetryableStep", saga => Task.CompletedTask, retryOptions: retryOptions);

        var definitions = builder.BuildStepDefinitions();

        // Assert
        definitions["RetryableStep"].ResiliencePipeline.ShouldNotBeNull();
    }

    [Fact]
    public void AddStep_MultipleSteps_MaintainsOrder()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder
            .AddStep("Step1", saga => Task.CompletedTask)
            .AddStep("Step2", saga => Task.CompletedTask)
            .AddStep("Step3", saga => Task.CompletedTask);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps.Count.ShouldBe(3);
        saga.Steps[0].Name.ShouldBe("Step1");
        saga.Steps[1].Name.ShouldBe("Step2");
        saga.Steps[2].Name.ShouldBe("Step3");
    }

    #endregion

    #region AddStepWithExponentialRetry Tests

    [Fact]
    public void AddStepWithExponentialRetry_CreatesStepWithRetryPipeline()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStepWithExponentialRetry(
            "HttpCallStep",
            saga => Task.CompletedTask,
            maxRetries: 5);

        var definitions = builder.BuildStepDefinitions();

        // Assert
        definitions["HttpCallStep"].ResiliencePipeline.ShouldNotBeNull();
    }

    [Fact]
    public void AddStepWithExponentialRetry_WithCompensation_SetsCompensationName()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddStepWithExponentialRetry(
            "CreateResource",
            saga => Task.CompletedTask,
            saga => Task.CompletedTask);

        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps[0].CompensationName.ShouldBe("CreateResource");
    }

    #endregion

    #region AddPreValidation Tests

    [Fact]
    public void AddPreValidation_AddsValidationToBuilder()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder.AddPreValidation("CheckBalance", saga =>
            Task.FromResult(SagaValidationResult.Success()));

        var validations = builder.BuildValidationDefinitions();

        // Assert
        validations.Count.ShouldBe(1);
        validations[0].Name.ShouldBe("CheckBalance");
    }

    [Fact]
    public void AddPreValidation_MultipleValidations_MaintainsOrder()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        builder
            .AddPreValidation("Validation1", saga => Task.FromResult(SagaValidationResult.Success()))
            .AddPreValidation("Validation2", saga => Task.FromResult(SagaValidationResult.Success()))
            .AddPreValidation("Validation3", saga => Task.FromResult(SagaValidationResult.Success()));

        var validations = builder.BuildValidationDefinitions();

        // Assert
        validations.Count.ShouldBe(3);
        validations[0].Name.ShouldBe("Validation1");
        validations[1].Name.ShouldBe("Validation2");
        validations[2].Name.ShouldBe("Validation3");
    }

    #endregion

    #region BuildSaga Tests

    [Fact]
    public void BuildSaga_CreatesSagaWithCorrectType()
    {
        // Arrange
        var builder = new SagaBuilder();
        builder.AddStep("Step1", saga => Task.CompletedTask);

        // Act
        var saga = builder.BuildSaga("OrderSaga");

        // Assert
        saga.SagaType.ShouldBe("OrderSaga");
    }

    [Fact]
    public void BuildSaga_InitializesSagaWithDefaults()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Id.ShouldNotBe(Guid.Empty);
        saga.Status.ShouldBe(SagaStatus.Pending);
        saga.Version.ShouldBe(0);
        saga.Steps.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSaga_WithNoSteps_CreatesEmptySaga()
    {
        // Arrange
        var builder = new SagaBuilder();

        // Act
        var saga = builder.BuildSaga("TestSaga");

        // Assert
        saga.Steps.ShouldBeEmpty();
    }

    #endregion

    #region BuildStepDefinitions Tests

    [Fact]
    public void BuildStepDefinitions_ReturnsDictionaryKeyedByStepName()
    {
        // Arrange
        var builder = new SagaBuilder();
        builder
            .AddStep("Step1", saga => Task.CompletedTask)
            .AddStep("Step2", saga => Task.CompletedTask);

        // Act
        var definitions = builder.BuildStepDefinitions();

        // Assert
        definitions.Keys.ShouldContain("Step1");
        definitions.Keys.ShouldContain("Step2");
        definitions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task BuildStepDefinitions_PreservesExecuteActions()
    {
        // Arrange
        var builder = new SagaBuilder();
        var executed = false;

        builder.AddStep("TestStep", saga =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Act
        var definitions = builder.BuildStepDefinitions();
        var action = definitions["TestStep"].ExecuteAction;
        await action(new Saga());

        // Assert
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task BuildStepDefinitions_PreservesCompensationActions()
    {
        // Arrange
        var builder = new SagaBuilder();
        var compensated = false;

        builder.AddStep(
            "TestStep",
            saga => Task.CompletedTask,
            saga =>
            {
                compensated = true;
                return Task.CompletedTask;
            });

        // Act
        var definitions = builder.BuildStepDefinitions();
        var compensationAction = definitions["TestStep"].CompensationAction;
        await compensationAction!(new Saga());

        // Assert
        compensated.ShouldBeTrue();
    }

    #endregion

    #region BuildValidationDefinitions Tests

    [Fact]
    public void BuildValidationDefinitions_ReturnsListInOrder()
    {
        // Arrange
        var builder = new SagaBuilder();
        builder
            .AddPreValidation("Val1", saga => Task.FromResult(SagaValidationResult.Success()))
            .AddPreValidation("Val2", saga => Task.FromResult(SagaValidationResult.Success()));

        // Act
        var validations = builder.BuildValidationDefinitions();

        // Assert
        validations[0].Name.ShouldBe("Val1");
        validations[1].Name.ShouldBe("Val2");
    }

    [Fact]
    public async Task BuildValidationDefinitions_PreservesValidationActions()
    {
        // Arrange
        var builder = new SagaBuilder();
        var executed = false;

        builder.AddPreValidation("TestValidation", saga =>
        {
            executed = true;
            return Task.FromResult(SagaValidationResult.Success());
        });

        // Act
        var validations = builder.BuildValidationDefinitions();
        var action = validations[0].ValidateAction;
        await action(new Saga());

        // Assert
        executed.ShouldBeTrue();
    }

    #endregion
}