using Shouldly;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.UnitTests.Domain;

public class SagaTests
{
    #region Cancellation Tests

    [Fact]
    public void RequestCancellation_SetsFlag()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var beforeRequest = DateTime.UtcNow;

        // Act
        saga.RequestCancellation();

        // Assert
        saga.IsCancellationRequested.ShouldBeTrue();
        saga.CancellationRequestedAt.ShouldNotBeNull();
        saga.CancellationRequestedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeRequest);
    }

    #endregion

    // Helper class for complex data tests
    private class OrderData
    {
        public Guid OrderId { get; set; }
        public List<string> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    #region Lock Acquisition Tests

    [Fact]
    public void TryAcquireLock_WhenUnlocked_ReturnsTrue()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var workerToken = Guid.NewGuid();

        // Act
        var result = saga.TryAcquireLock(workerToken, TimeSpan.FromMinutes(5));

        // Assert
        result.ShouldBeTrue();
        saga.LockToken.ShouldBe(workerToken);
        saga.LockedAt.ShouldNotBeNull();
    }

    [Fact]
    public void TryAcquireLock_WhenAlreadyLockedByAnotherWorker_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var firstWorkerToken = Guid.NewGuid();
        var secondWorkerToken = Guid.NewGuid();

        saga.TryAcquireLock(firstWorkerToken, TimeSpan.FromMinutes(5));

        // Act
        var result = saga.TryAcquireLock(secondWorkerToken, TimeSpan.FromMinutes(5));

        // Assert
        result.ShouldBeFalse();
        saga.LockToken.ShouldBe(firstWorkerToken); // Still locked by first worker
    }

    [Fact]
    public void TryAcquireLock_WhenLockExpired_ReturnsTrue()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var firstWorkerToken = Guid.NewGuid();
        var secondWorkerToken = Guid.NewGuid();

        saga.TryAcquireLock(firstWorkerToken, TimeSpan.FromMinutes(5));

        // Simulate expired lock
        saga.LockedAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var result = saga.TryAcquireLock(secondWorkerToken, TimeSpan.FromMinutes(5));

        // Assert
        result.ShouldBeTrue();
        saga.LockToken.ShouldBe(secondWorkerToken);
    }

    [Fact]
    public void ReleaseLock_WithCorrectToken_ClearsLock()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var workerToken = Guid.NewGuid();
        saga.TryAcquireLock(workerToken, TimeSpan.FromMinutes(5));

        // Act
        saga.ReleaseLock(workerToken);

        // Assert
        saga.LockToken.ShouldBeNull();
        saga.LockedAt.ShouldBeNull();
    }

    [Fact]
    public void ReleaseLock_WithIncorrectToken_DoesNotClearLock()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var correctToken = Guid.NewGuid();
        var incorrectToken = Guid.NewGuid();
        saga.TryAcquireLock(correctToken, TimeSpan.FromMinutes(5));

        // Act
        saga.ReleaseLock(incorrectToken);

        // Assert
        saga.LockToken.ShouldBe(correctToken); // Still locked
        saga.LockedAt.ShouldNotBeNull();
    }

    #endregion

    #region Timeout Tests

    [Fact]
    public void SetTimeout_SetsTimeoutAndDeadline()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var timeout = TimeSpan.FromHours(2);

        // Act
        saga.SetTimeout(timeout);

        // Assert
        saga.Timeout.ShouldBe(timeout);
        saga.TimeoutDeadline.ShouldNotBeNull();
        saga.TimeoutDeadline.Value.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public void IsTimedOut_WhenDeadlineNotSet_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        var result = saga.IsTimedOut();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTimedOut_WhenDeadlineInFuture_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        saga.SetTimeout(TimeSpan.FromHours(1));

        // Act
        var result = saga.IsTimedOut();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTimedOut_WhenDeadlinePassed_ReturnsTrue()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            TimeoutDeadline = DateTime.UtcNow.AddMinutes(-10)
        };

        // Act
        var result = saga.IsTimedOut();

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Data Storage Tests

    [Fact]
    public void SetData_StoresValue()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var testData = new { CustomerId = Guid.NewGuid(), Amount = 100.50m };

        // Act
        saga.SetData("orderData", testData);

        // Assert
        saga.SagaData.ShouldNotBe("{}");
    }

    [Fact]
    public void GetData_RetrievesStoredValue()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var customerId = Guid.NewGuid();
        saga.SetData("customerId", customerId);

        // Act
        var result = saga.GetData<Guid>("customerId");

        // Assert
        result.ShouldBe(customerId);
    }

    [Fact]
    public void GetData_WhenKeyNotFound_ReturnsDefault()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        var result = saga.GetData<string>("nonExistentKey");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void SetData_OverwritesExistingValue()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        saga.SetData("counter", 1);

        // Act
        saga.SetData("counter", 2);
        var result = saga.GetData<int>("counter");

        // Assert
        result.ShouldBe(2);
    }

    [Fact]
    public void GetData_SupportsComplexTypes()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var complexData = new OrderData
        {
            OrderId = Guid.NewGuid(),
            Items = ["Item1", "Item2"],
            TotalAmount = 250.75m
        };

        saga.SetData("orderData", complexData);

        // Act
        var result = saga.GetData<OrderData>("orderData");

        // Assert
        result.ShouldNotBeNull();
        result.OrderId.ShouldBe(complexData.OrderId);
        result.Items.ShouldBe(complexData.Items);
        result.TotalAmount.ShouldBe(complexData.TotalAmount);
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void MarkAsRunning_UpdatesStatus()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        saga.MarkAsRunning();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Running);
    }

    [Fact]
    public void MarkAsCompleted_UpdatesStatusAndCompletedAt()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var beforeCompletion = DateTime.UtcNow;

        // Act
        saga.MarkAsCompleted();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Completed);
        saga.CompletedAt.ShouldNotBeNull();
        saga.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeCompletion);
    }

    [Fact]
    public void MarkAsFailed_UpdatesStatusErrorMessageAndCompletedAt()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var errorMessage = "Step X failed";

        // Act
        saga.MarkAsFailed(errorMessage);

        // Assert
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.ErrorMessage.ShouldBe(errorMessage);
        saga.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsFailedAfterPonr_UpdatesStatusErrorMessageAndCompletedAt()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };
        var errorMessage = "Critical failure after PONR";

        // Act
        saga.MarkAsFailedAfterPonr(errorMessage);

        // Assert
        saga.Status.ShouldBe(SagaStatus.FailedAfterPonr);
        saga.ErrorMessage.ShouldBe(errorMessage);
        saga.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsCompensating_UpdatesStatus()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        saga.MarkAsCompensating();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensating);
    }

    [Fact]
    public void MarkAsCompensated_UpdatesStatusAndCompletedAt()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        saga.MarkAsCompensated();

        // Assert
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsWaitingForCallback_UpdatesStatus()
    {
        // Arrange
        var saga = new Saga { SagaType = "Test" };

        // Act
        saga.MarkAsWaitingForCallback();

        // Assert
        saga.Status.ShouldBe(SagaStatus.WaitingForCallback);
    }

    #endregion

    #region Step Management Tests

    [Fact]
    public void MarkStepAsRunning_UpdatesStepStatusAndStartedAt()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps = [new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending }]
        };

        var beforeStart = DateTime.UtcNow;

        // Act
        saga.MarkStepAsRunning("Step1");

        // Assert
        var step = saga.Steps[0];
        step.Status.ShouldBe(SagaStepStatus.Running);
        step.StartedAt.ShouldNotBeNull();
        step.StartedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeStart);
    }

    [Fact]
    public void MarkStepAsCompleted_UpdatesStepStatusAndCompletedAt()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps = [new SagaStep { Name = "Step1", Status = SagaStepStatus.Running }]
        };

        var beforeCompletion = DateTime.UtcNow;

        // Act
        saga.MarkStepAsCompleted("Step1");

        // Assert
        var step = saga.Steps[0];
        step.Status.ShouldBe(SagaStepStatus.Completed);
        step.CompletedAt.ShouldNotBeNull();
        step.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeCompletion);
    }

    [Fact]
    public void MarkStepAsFailed_UpdatesStepStatusErrorMessageAndCompletedAt()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps = [new SagaStep { Name = "Step1", Status = SagaStepStatus.Running }]
        };

        var errorMessage = "Network timeout";

        // Act
        saga.MarkStepAsFailed("Step1", errorMessage);

        // Assert
        var step = saga.Steps[0];
        step.Status.ShouldBe(SagaStepStatus.Failed);
        step.ErrorMessage.ShouldBe(errorMessage);
        step.CompletedAt.ShouldNotBeNull();
    }
    
    [Fact]
    public void MarkAsScheduled_SetsStatusToScheduled()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga" };

        // Act
        saga.Schedule(null);

        // Assert
        saga.Status.ShouldBe(SagaStatus.Scheduled);
    }

    #endregion

    #region Point of No Return Tests

    [Fact]
    public void HasReachedPointOfNoReturn_WhenNoPonrSteps_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps = [new SagaStep { Name = "Step1", Type = SagaStepType.Execution, Status = SagaStepStatus.Completed }]
        };

        // Act
        var result = saga.HasReachedPointOfNoReturn();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasReachedPointOfNoReturn_WhenPonrStepPending_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps =
            [
                new SagaStep { Name = "PONR", Type = SagaStepType.PointOfNoReturn, Status = SagaStepStatus.Pending }
            ]
        };

        // Act
        var result = saga.HasReachedPointOfNoReturn();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasReachedPointOfNoReturn_WhenPonrStepCompleted_ReturnsTrue()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps =
            [
                new SagaStep { Name = "PONR", Type = SagaStepType.PointOfNoReturn, Status = SagaStepStatus.Completed }
            ]
        };

        // Act
        var result = saga.HasReachedPointOfNoReturn();

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Compensation Tests

    [Fact]
    public void GetStepsNeedingCompensation_ReturnsCompletedCompensableStepsInReverseOrder()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps =
            [
                new SagaStep
                {
                    Name = "Step1", Status = SagaStepStatus.Completed, Type = SagaStepType.Execution,
                    CompensationName = "Compensate1"
                },
                new SagaStep
                {
                    Name = "Step2", Status = SagaStepStatus.Completed, Type = SagaStepType.Execution,
                    CompensationName = "Compensate2"
                },
                new SagaStep
                {
                    Name = "Step3", Status = SagaStepStatus.Pending, Type = SagaStepType.Execution,
                    CompensationName = "Compensate3"
                }
            ]
        };

        // Act
        var steps = saga.GetStepsNeedingCompensation().ToList();

        // Assert
        steps.Count.ShouldBe(2);
        steps[0].Name.ShouldBe("Step2"); // Reversed order
        steps[1].Name.ShouldBe("Step1");
    }

    [Fact]
    public void GetStepsNeedingCompensation_ExcludesNoCompensationSteps()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps =
            [
                new SagaStep { Name = "Step1", Status = SagaStepStatus.Completed, Type = SagaStepType.NoCompensation },
                new SagaStep
                {
                    Name = "Step2", Status = SagaStepStatus.Completed, Type = SagaStepType.Execution,
                    CompensationName = "Compensate2"
                }
            ]
        };

        // Act
        var steps = saga.GetStepsNeedingCompensation().ToList();

        // Assert
        steps.Count.ShouldBe(1);
        steps[0].Name.ShouldBe("Step2");
    }

    [Fact]
    public void GetStepsNeedingCompensation_ExcludesPonrSteps()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "Test",
            Steps =
            [
                new SagaStep
                {
                    Name = "Step1", Status = SagaStepStatus.Completed, Type = SagaStepType.Execution,
                    CompensationName = "Compensate1"
                },
                new SagaStep { Name = "PONR", Status = SagaStepStatus.Completed, Type = SagaStepType.PointOfNoReturn }
            ]
        };

        // Act
        var steps = saga.GetStepsNeedingCompensation().ToList();

        // Assert
        steps.Count.ShouldBe(1);
        steps[0].Name.ShouldBe("Step1");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var saga = new Saga();

        // Assert
        saga.Id.ShouldNotBe(Guid.Empty);
        saga.Status.ShouldBe(SagaStatus.Pending);
        saga.Version.ShouldBe(0);
        saga.SagaData.ShouldBe("{}");
        saga.Steps.ShouldNotBeNull();
        saga.Steps.ShouldBeEmpty();
        saga.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithParameters_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sagaType = "TestSaga";
        var status = SagaStatus.Running;
        var sagaData = "{\"key\":\"value\"}";
        var steps = new List<SagaStep> { new() { Name = "Step1" } };
        var errorMessage = "Test error";
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var completedAt = DateTime.UtcNow;
        var version = 5;
        var lockToken = Guid.NewGuid();
        var lockedAt = DateTime.UtcNow.AddMinutes(-5);
        var correlationId = "correlation-123";
        var traceId = "trace-456";
        var timeout = TimeSpan.FromHours(2);
        var timeoutDeadline = DateTime.UtcNow.AddHours(1);

        // Act
        var saga = new Saga
        {
            Id = id,
            SagaType = sagaType,
            Status = status,
            SagaData = sagaData,
            Steps = steps,
            ErrorMessage = errorMessage,
            CreatedAt = createdAt,
            CompletedAt = completedAt,
            Version = version,
            LockToken = lockToken,
            LockedAt = lockedAt,
            CorrelationId = correlationId,
            TraceId = traceId,
            Timeout = timeout,
            TimeoutDeadline = timeoutDeadline,
            IsCancellationRequested = true
        };

        // Assert
        saga.Id.ShouldBe(id);
        saga.SagaType.ShouldBe(sagaType);
        saga.Status.ShouldBe(status);
        saga.SagaData.ShouldBe(sagaData);
        saga.Steps.ShouldBe(steps);
        saga.ErrorMessage.ShouldBe(errorMessage);
        saga.CreatedAt.ShouldBe(createdAt);
        saga.CompletedAt.ShouldBe(completedAt);
        saga.Version.ShouldBe(version);
        saga.LockToken.ShouldBe(lockToken);
        saga.LockedAt.ShouldBe(lockedAt);
        saga.CorrelationId.ShouldBe(correlationId);
        saga.TraceId.ShouldBe(traceId);
        saga.IsCancellationRequested.ShouldBeTrue();
        saga.Timeout.ShouldBe(timeout);
        saga.TimeoutDeadline.ShouldBe(timeoutDeadline);
    }

    #endregion
}