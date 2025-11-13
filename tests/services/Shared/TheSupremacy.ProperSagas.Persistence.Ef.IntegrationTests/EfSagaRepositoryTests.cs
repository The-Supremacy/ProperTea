using Shouldly;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Persistence.Ef.IntegrationTests;

[Collection("Database")]
public class EfSagaRepositoryTests : IAsyncLifetime
{
    private readonly SagaDatabaseFixture _fixture;
    private SagaDbContext _context = null!;
    private EfSagaRepository<SagaDbContext> _repository = null!;

    public EfSagaRepositoryTests(SagaDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _repository = new EfSagaRepository<SagaDbContext>(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        var sagas = _context.Sagas.ToList();
        _context.Sagas.RemoveRange(sagas);
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
    }

    #region Complex Scenario Tests

    [Fact]
    public async Task CompleteWorkflow_PersistsCorrectly()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "OrderSaga",
            CorrelationId = "order-123",
            Steps =
            [
                new SagaStep
                {
                    Name = "ReserveInventory",
                    Type = SagaStepType.Execution,
                    CompensationName = "ReleaseInventory"
                },
                new SagaStep
                {
                    Name = "ChargePayment",
                    Type = SagaStepType.Execution,
                    CompensationName = "RefundPayment"
                },
                new SagaStep { Name = "CommitOrder", Type = SagaStepType.PointOfNoReturn }
            ]
        };
        saga.SetData("orderId", Guid.NewGuid());
        saga.SetData("amount", 250.75m);

        // Act - Execute workflow
        await _repository.AddAsync(saga);

        saga.MarkAsRunning();
        await _repository.TryUpdateAsync(saga);

        saga.MarkStepAsRunning("ReserveInventory");
        await _repository.TryUpdateAsync(saga);

        saga.MarkStepAsCompleted("ReserveInventory");
        await _repository.TryUpdateAsync(saga);

        saga.MarkStepAsRunning("ChargePayment");
        await _repository.TryUpdateAsync(saga);

        saga.MarkStepAsFailed("ChargePayment", "Payment declined");
        saga.MarkAsFailed("Payment failed");
        await _repository.TryUpdateAsync(saga);

        // Compensation
        saga.MarkAsCompensating();
        await _repository.TryUpdateAsync(saga);

        saga.Steps[0].Status = SagaStepStatus.Compensated;
        saga.MarkAsCompensated();
        await _repository.TryUpdateAsync(saga);

        // Assert
        var final = await _repository.GetByIdAsync(saga.Id);
        final.ShouldNotBeNull();
        final.Status.ShouldBe(SagaStatus.Compensated);
        final.Steps[0].Status.ShouldBe(SagaStepStatus.Compensated);
        final.Steps[1].Status.ShouldBe(SagaStepStatus.Failed);
        final.Steps[1].ErrorMessage.ShouldBe("Payment declined");
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_AddsSagaToDatabase()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "OrderSaga",
            Status = SagaStatus.Pending
        };

        // Act
        await _repository.AddAsync(saga);

        // Assert
        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(saga.Id);
        retrieved.SagaType.ShouldBe("OrderSaga");
    }

    [Fact]
    public async Task AddAsync_PersistsAllProperties()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "PaymentSaga",
            Status = SagaStatus.Running,
            CorrelationId = "corr-123",
            TraceId = "trace-456"
        };
        saga.SetData("amount", 100.50m);
        saga.SetTimeout(TimeSpan.FromHours(1));

        // Act
        await _repository.AddAsync(saga);

        // Assert
        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Status.ShouldBe(SagaStatus.Running);
        retrieved.CorrelationId.ShouldBe("corr-123");
        retrieved.TraceId.ShouldBe("trace-456");
        retrieved.GetData<decimal>("amount").ShouldBe(100.50m);

        // Use tolerance for TimeSpan comparison due to DB precision
        retrieved.Timeout.ShouldNotBeNull();
        Math.Abs((retrieved.Timeout!.Value - TimeSpan.FromHours(1)).TotalMilliseconds).ShouldBeLessThan(100);
    }

    [Fact]
    public async Task AddAsync_PersistsSteps()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "OrderSaga",
            Steps =
            [
                new SagaStep
                {
                    Name = "Step1",
                    Status = SagaStepStatus.Completed,
                    Type = SagaStepType.Execution,
                    CompensationName = "Compensate1"
                },
                new SagaStep
                {
                    Name = "Step2",
                    Status = SagaStepStatus.Pending,
                    Type = SagaStepType.PointOfNoReturn
                }
            ]
        };

        // Act
        await _repository.AddAsync(saga);

        // Assert
        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Steps.Count.ShouldBe(2);
        retrieved.Steps[0].Name.ShouldBe("Step1");
        retrieved.Steps[0].Status.ShouldBe(SagaStepStatus.Completed);
        retrieved.Steps[0].CompensationName.ShouldBe("Compensate1");
        retrieved.Steps[1].Name.ShouldBe("Step2");
        retrieved.Steps[1].Type.ShouldBe(SagaStepType.PointOfNoReturn);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenSagaExists_ReturnsSaga()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga" };
        await _repository.AddAsync(saga);

        // Act
        var result = await _repository.GetByIdAsync(saga.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(saga.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSagaDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_LoadsSteps()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Steps = [new SagaStep { Name = "Step1" }]
        };
        await _repository.AddAsync(saga);

        // Act
        var result = await _repository.GetByIdAsync(saga.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Steps.ShouldNotBeEmpty();
        result.Steps[0].Name.ShouldBe("Step1");
    }

    #endregion

    #region TryUpdateAsync Tests

    [Fact]
    public async Task TryUpdateAsync_WhenVersionMatches_UpdatesAndReturnsTrue()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Version = 0 };
        await _repository.AddAsync(saga);

        // Act
        saga.MarkAsRunning();
        var result = await _repository.TryUpdateAsync(saga);

        // Assert
        result.ShouldBeTrue();
        saga.Version.ShouldBe(1);

        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved!.Status.ShouldBe(SagaStatus.Running);
        retrieved.Version.ShouldBe(1);
    }

    [Fact]
    public async Task TryUpdateAsync_WhenVersionMismatch_ReturnsFalse()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga" };
        await _repository.AddAsync(saga);

        // Simulate concurrent update
        var sagaCopy = await _repository.GetByIdAsync(saga.Id);
        sagaCopy!.MarkAsRunning();
        await _repository.TryUpdateAsync(sagaCopy);

        // Act - Try to update original (outdated version)
        saga.MarkAsCompleted();
        var result = await _repository.TryUpdateAsync(saga);

        // Assert
        result.ShouldBeFalse();

        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved!.Status.ShouldBe(SagaStatus.Running); // Should be the first update
    }

    [Fact]
    public async Task TryUpdateAsync_UpdatesAllModifiableFields()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Steps =
            [
                new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending }
            ]
        };
        await _repository.AddAsync(saga);

        // Act
        saga.MarkAsFailed("Test error");
        saga.SetData("errorDetails", "Details");
        saga.MarkStepAsFailed("Step1", "Step error");
        var lockToken = Guid.NewGuid();
        saga.TryAcquireLock(lockToken, TimeSpan.FromMinutes(5));

        await _repository.TryUpdateAsync(saga);

        // Assert
        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Status.ShouldBe(SagaStatus.Failed);
        retrieved.ErrorMessage.ShouldBe("Test error");
        retrieved.GetData<string>("errorDetails").ShouldBe("Details");
        retrieved.LockToken.ShouldBe(lockToken);
        retrieved.LockedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task TryUpdateAsync_HandlesMultipleStepUpdates()
    {
        // Arrange
        var saga = new Saga
        {
            SagaType = "TestSaga",
            Steps =
            [
                new SagaStep { Name = "Step1", Status = SagaStepStatus.Pending },
                new SagaStep { Name = "Step2", Status = SagaStepStatus.Pending }
            ]
        };
        await _repository.AddAsync(saga);

        // Act
        saga.MarkStepAsRunning("Step1");
        await _repository.TryUpdateAsync(saga);

        saga.MarkStepAsCompleted("Step1");
        saga.MarkStepAsRunning("Step2");
        await _repository.TryUpdateAsync(saga);

        // Assert
        var retrieved = await _repository.GetByIdAsync(saga.Id);
        retrieved!.Steps[0].Status.ShouldBe(SagaStepStatus.Completed);
        retrieved.Steps[1].Status.ShouldBe(SagaStepStatus.Running);
    }

    #endregion

    #region FindFailedSagasAsync Tests

    [Fact]
    public async Task FindFailedSagasAsync_ReturnsOnlyFailedSagas()
    {
        // Arrange
        await _repository.AddAsync(new Saga { SagaType = "Saga1", Status = SagaStatus.Failed });
        await _repository.AddAsync(new Saga { SagaType = "Saga2", Status = SagaStatus.Completed });
        await _repository.AddAsync(new Saga { SagaType = "Saga3", Status = SagaStatus.Failed });
        await _repository.AddAsync(new Saga { SagaType = "Saga4", Status = SagaStatus.Running });

        // Act
        var result = await _repository.FindFailedSagasAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.Status == SagaStatus.Failed);
    }

    [Fact]
    public async Task FindFailedSagasAsync_WhenNoFailedSagas_ReturnsEmptyList()
    {
        // Arrange
        await _repository.AddAsync(new Saga { SagaType = "Saga1", Status = SagaStatus.Completed });

        // Act
        var result = await _repository.FindFailedSagasAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentUpdates_OnlyOneSucceeds()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga" };
        await _repository.AddAsync(saga);

        // Act - Simulate two workers trying to update
        var saga1 = await _repository.GetByIdAsync(saga.Id);
        var saga2 = await _repository.GetByIdAsync(saga.Id);

        saga1!.MarkAsRunning();
        saga2!.MarkAsRunning();

        var result1 = await _repository.TryUpdateAsync(saga1);
        var result2 = await _repository.TryUpdateAsync(saga2);

        // Assert
        (result1 || result2).ShouldBeTrue(); // At least one succeeds
        (result1 && result2).ShouldBeFalse(); // But not both
    }

    [Fact]
    public async Task LockAcquisition_PreventsConcurrentExecution()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga" };
        await _repository.AddAsync(saga);

        var token1 = Guid.NewGuid();
        var token2 = Guid.NewGuid();

        // Act
        var saga1 = await _repository.GetByIdAsync(saga.Id);
        saga1!.TryAcquireLock(token1, TimeSpan.FromMinutes(5));
        await _repository.TryUpdateAsync(saga1);

        var saga2 = await _repository.GetByIdAsync(saga.Id);
        var lockAcquired = saga2!.TryAcquireLock(token2, TimeSpan.FromMinutes(5));

        // Assert
        lockAcquired.ShouldBeFalse();
    }

    #endregion

    #region FindSagasNeedingResumptionAsync Tests

    [Fact]
    public async Task FindSagasNeedingResumptionAsync_ReturnsUnlockedRunningSagas()
    {
        // Arrange
        await _repository.AddAsync(new Saga { SagaType = "Saga1", Status = SagaStatus.Running });
        await _repository.AddAsync(new Saga { SagaType = "Saga2", Status = SagaStatus.WaitingForCallback });
        await _repository.AddAsync(new Saga { SagaType = "Saga3", Status = SagaStatus.Completed });

        // Act
        var result = await _repository.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.Status == SagaStatus.Running);
        result.ShouldContain(s => s.Status == SagaStatus.WaitingForCallback);
    }

    [Fact]
    public async Task FindSagasNeedingResumptionAsync_ExcludesRecentlyLockedSagas()
    {
        // Arrange
        var lockedSaga = new Saga { SagaType = "LockedSaga", Status = SagaStatus.Running };
        lockedSaga.TryAcquireLock(Guid.NewGuid(), TimeSpan.FromMinutes(5));
        await _repository.AddAsync(lockedSaga);

        var unlockedSaga = new Saga { SagaType = "UnlockedSaga", Status = SagaStatus.Running };
        await _repository.AddAsync(unlockedSaga);

        // Act
        var result = await _repository.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(unlockedSaga.Id);
    }

    [Fact]
    public async Task FindSagasNeedingResumptionAsync_IncludesStaleLocks()
    {
        // Arrange
        var staleLockSaga = new Saga { SagaType = "StaleLock", Status = SagaStatus.Running };
        staleLockSaga.TryAcquireLock(Guid.NewGuid(), TimeSpan.FromMinutes(10));
        staleLockSaga.LockedAt = DateTime.UtcNow.AddMinutes(-6); // Lock is stale
        await _repository.AddAsync(staleLockSaga);

        // Act - Lock timeout is 5 minutes
        var result = await _repository.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(staleLockSaga.Id);
    }

    [Fact]
    public async Task FindSagasNeedingResumptionAsync_RespectsLockTimeout()
    {
        // Arrange
        var saga = new Saga { SagaType = "TestSaga", Status = SagaStatus.Running };
        saga.TryAcquireLock(Guid.NewGuid(), TimeSpan.FromMinutes(10));
        saga.LockedAt = DateTime.UtcNow.AddMinutes(-3);
        await _repository.AddAsync(saga);

        // Act
        var resultShortTimeout = await _repository.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(2));
        var resultLongTimeout = await _repository.FindSagasNeedingResumptionAsync(TimeSpan.FromMinutes(5));

        // Assert
        resultShortTimeout.Count.ShouldBe(1); // 3 min old lock > 2 min timeout
        resultLongTimeout.ShouldBeEmpty(); // 3 min old lock < 5 min timeout
    }

    #endregion

    #region FindTimedOutSagasAsync Tests

    [Fact]
    public async Task FindTimedOutSagasAsync_ReturnsOnlyTimedOutSagas()
    {
        // Arrange
        var timedOut1 = new Saga { SagaType = "TimedOut1", Status = SagaStatus.Running };
        timedOut1.SetTimeout(TimeSpan.FromHours(1));
        timedOut1.TimeoutDeadline = DateTime.UtcNow.AddHours(-1); // Expired
        await _repository.AddAsync(timedOut1);

        var notTimedOut = new Saga { SagaType = "NotTimedOut", Status = SagaStatus.Running };
        notTimedOut.SetTimeout(TimeSpan.FromHours(1));
        notTimedOut.TimeoutDeadline = DateTime.UtcNow.AddHours(1); // Not expired
        await _repository.AddAsync(notTimedOut);

        var timedOut2 = new Saga { SagaType = "TimedOut2", Status = SagaStatus.Running };
        timedOut2.SetTimeout(TimeSpan.FromMinutes(30));
        timedOut2.TimeoutDeadline = DateTime.UtcNow.AddMinutes(-5); // Expired
        await _repository.AddAsync(timedOut2);

        // Act
        var result = await _repository.FindTimedOutSagasAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.Id == timedOut1.Id);
        result.ShouldContain(s => s.Id == timedOut2.Id);
        result.ShouldNotContain(s => s.Id == notTimedOut.Id);
    }

    [Fact]
    public async Task FindTimedOutSagasAsync_OnlyIncludesActiveSagas()
    {
        // Arrange
        var completedTimedOut = new Saga { SagaType = "CompletedTimedOut", Status = SagaStatus.Completed };
        completedTimedOut.SetTimeout(TimeSpan.FromHours(1));
        completedTimedOut.TimeoutDeadline = DateTime.UtcNow.AddHours(-1);
        await _repository.AddAsync(completedTimedOut);

        var runningTimedOut = new Saga { SagaType = "RunningTimedOut", Status = SagaStatus.Running };
        runningTimedOut.SetTimeout(TimeSpan.FromHours(1));
        runningTimedOut.TimeoutDeadline = DateTime.UtcNow.AddHours(-1);
        await _repository.AddAsync(runningTimedOut);

        // Act
        var result = await _repository.FindTimedOutSagasAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(runningTimedOut.Id);
    }

    [Fact]
    public async Task FindTimedOutSagasAsync_ExcludesSagasWithoutTimeout()
    {
        // Arrange
        var sagaWithoutTimeout = new Saga { SagaType = "NoTimeout", Status = SagaStatus.Running };
        await _repository.AddAsync(sagaWithoutTimeout);

        // Act
        var result = await _repository.FindTimedOutSagasAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindTimedOutSagasAsync_WhenNoTimedOutSagas_ReturnsEmptyList()
    {
        // Arrange
        var saga = new Saga { SagaType = "Active", Status = SagaStatus.Running };
        saga.SetTimeout(TimeSpan.FromHours(1));
        await _repository.AddAsync(saga);

        // Act
        var result = await _repository.FindTimedOutSagasAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region FindSagasByCorrelationIdAsync Tests

    [Fact]
    public async Task FindSagasByCorrelationIdAsync_ReturnsSagasWithMatchingCorrelationId()
    {
        // Arrange
        var correlationId = "order-12345";
        await _repository.AddAsync(new Saga { SagaType = "Saga1", CorrelationId = correlationId });
        await _repository.AddAsync(new Saga { SagaType = "Saga2", CorrelationId = "other-id" });
        await _repository.AddAsync(new Saga { SagaType = "Saga3", CorrelationId = correlationId });

        // Act
        var result = await _repository.FindSagasByCorrelationIdAsync(correlationId);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.CorrelationId == correlationId);
    }

    [Fact]
    public async Task FindSagasByCorrelationIdAsync_WhenNoMatch_ReturnsEmptyList()
    {
        // Arrange
        await _repository.AddAsync(new Saga { SagaType = "Saga1", CorrelationId = "order-123" });

        // Act
        var result = await _repository.FindSagasByCorrelationIdAsync("non-existent");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindSagasByCorrelationIdAsync_IncludesSteps()
    {
        // Arrange
        var correlationId = "order-789";
        var saga = new Saga
        {
            SagaType = "TestSaga",
            CorrelationId = correlationId,
            Steps = [new SagaStep { Name = "Step1" }]
        };
        await _repository.AddAsync(saga);

        // Act
        var result = await _repository.FindSagasByCorrelationIdAsync(correlationId);

        // Assert
        result[0].Steps.ShouldNotBeEmpty();
        result[0].Steps[0].Name.ShouldBe("Step1");
    }

    [Fact]
    public async Task FindSagasByCorrelationIdAsync_IsCaseSensitive()
    {
        // Arrange
        var correlationId = "Order-ABC";
        await _repository.AddAsync(new Saga { SagaType = "Saga1", CorrelationId = correlationId });

        // Act
        var resultLower = await _repository.FindSagasByCorrelationIdAsync("order-abc");
        var resultExact = await _repository.FindSagasByCorrelationIdAsync(correlationId);

        // Assert
        resultLower.ShouldBeEmpty();
        resultExact.Count.ShouldBe(1);
    }

    #endregion
    
    #region FindScheduledSagasAsync Tests

[Fact]
public async Task FindScheduledSagasAsync_ReturnsOnlyScheduledSagas()
{
    // Arrange
    await _repository.AddAsync(new Saga { SagaType = "Saga1", Status = SagaStatus.Scheduled });
    await _repository.AddAsync(new Saga { SagaType = "Saga2", Status = SagaStatus.Running });
    await _repository.AddAsync(new Saga { SagaType = "Saga3", Status = SagaStatus.Scheduled });

    // Act
    var result = await _repository.FindScheduledSagasAsync(DateTime.UtcNow.AddHours(1));

    // Assert
    result.Count.ShouldBe(2);
    result.ShouldAllBe(s => s.Status == SagaStatus.Scheduled);
}

[Fact]
public async Task FindScheduledSagasAsync_RespectsScheduledForDate()
{
    // Arrange
    var now = DateTime.UtcNow;
    var futureScheduled = new Saga { SagaType = "Future", Status = SagaStatus.Scheduled };
    futureScheduled.ScheduledFor = now.AddHours(2);
    await _repository.AddAsync(futureScheduled);

    var readyScheduled = new Saga { SagaType = "Ready", Status = SagaStatus.Scheduled };
    readyScheduled.ScheduledFor = now.AddMinutes(-5);
    await _repository.AddAsync(readyScheduled);

    // Act
    var result = await _repository.FindScheduledSagasAsync(now);

    // Assert
    result.Count.ShouldBe(1);
    result[0].Id.ShouldBe(readyScheduled.Id);
}

[Fact]
public async Task FindScheduledSagasAsync_IncludesSagasWithoutScheduledFor()
{
    // Arrange
    var scheduledNoDate = new Saga { SagaType = "NoDate", Status = SagaStatus.Scheduled };
    await _repository.AddAsync(scheduledNoDate);

    // Act
    var result = await _repository.FindScheduledSagasAsync(DateTime.UtcNow);

    // Assert
    result.Count.ShouldBe(1);
    result[0].Id.ShouldBe(scheduledNoDate.Id);
}

[Fact]
public async Task FindScheduledSagasAsync_WhenNoScheduledSagas_ReturnsEmptyList()
{
    // Arrange
    await _repository.AddAsync(new Saga { SagaType = "Running", Status = SagaStatus.Running });

    // Act
    var result = await _repository.FindScheduledSagasAsync(DateTime.UtcNow);

    // Assert
    result.ShouldBeEmpty();
}

#endregion
}