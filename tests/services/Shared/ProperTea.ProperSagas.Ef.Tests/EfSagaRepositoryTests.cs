using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.ProperSagas.Ef.Tests.Setup;
using Shouldly;

namespace ProperTea.ProperSagas.Ef.Tests;

[Collection("DatabaseCollection")]
public class EfSagaRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public EfSagaRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(EfSagaRepository<TestDbContext> repository, TestDbContext dbContext)> GetRepositoryAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString,
                o => o.MigrationsAssembly(typeof(TestDbContext).Assembly.FullName)));

        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfSagaRepository<TestDbContext>(dbContext);
        return (repository, dbContext);
    }

    // FindByStatusAsync tests
    [Fact]
    public async Task FindByStatusAsync_MatchingStatus_ReturnsSagasWithStatus()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();

        var saga1 = new TestSaga();
        saga1.MarkAsRunning();
        await repository.SaveAsync(saga1);

        var saga2 = new TestSaga();
        saga2.MarkAsWaitingForCallback("approval");
        await repository.SaveAsync(saga2);

        var saga3 = new TestSaga();
        saga3.MarkAsCompleted();
        await repository.SaveAsync(saga3);

        var saga4 = new TestSaga();
        saga4.MarkAsWaitingForCallback("payment");
        await repository.SaveAsync(saga4);

        // Act
        var waitingSagas = await repository.FindByStatusAsync(SagaStatus.WaitingForCallback);

        // Assert
        waitingSagas.Count.ShouldBe(2);
        waitingSagas.ShouldContain(saga2.Id);
        waitingSagas.ShouldContain(saga4.Id);
    }

    // GetByIdAsync tests
    [Fact]
    public async Task GetByIdAsync_ExistingSaga_ReturnsSagaWithCorrectData()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var originalSaga = new TestSaga();
        var userId = Guid.NewGuid();
        originalSaga.SetUserId(userId);
        originalSaga.MarkAsRunning();

        await repository.SaveAsync(originalSaga);

        // Act
        var retrievedSaga = await repository.GetByIdAsync<TestSaga>(originalSaga.Id);

        // Assert
        retrievedSaga.ShouldNotBeNull();
        retrievedSaga.Id.ShouldBe(originalSaga.Id);
        retrievedSaga.Status.ShouldBe(SagaStatus.Running);
        retrievedSaga.GetUserId().ShouldBe(userId);
        retrievedSaga.Steps.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentSaga_ReturnsNull()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var nonexistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync<TestSaga>(nonexistentId);

        // Assert
        result.ShouldBeNull();
    }

    // SaveAsync tests
    [Fact]
    public async Task SaveAsync_NewSaga_PersistsSagaToDatabase()
    {
        // Arrange
        var (repository, context) = await GetRepositoryAsync();
        var saga = new TestSaga();
        saga.SetUserId(Guid.NewGuid());
        saga.MarkAsRunning();

        // Act
        await repository.SaveAsync(saga);

        // Assert
        var savedEntity = await context.Sagas.FindAsync(saga.Id);
        savedEntity.ShouldNotBeNull();
        savedEntity.Id.ShouldBe(saga.Id);
        savedEntity.SagaType.ShouldBe(nameof(TestSaga));
        savedEntity.Status.ShouldBe(SagaStatus.Running.ToString());
        savedEntity.SagaData.ShouldContain("userId");
    }

    [Fact]
    public async Task SaveAsync_SagaWithAllStepProperties_PersistsAllProperties()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var saga = new TestSaga();

        saga.Steps[0].Status = SagaStepStatus.Completed;
        saga.Steps[0].IsPreValidation = true;
        saga.Steps[0].HasCompensation = false;
        saga.Steps[0].CompensationName = "UndoStep1";
        saga.Steps[0].StartedAt = DateTime.UtcNow;
        saga.Steps[0].CompletedAt = DateTime.UtcNow;
        saga.Steps[0].ErrorMessage = "Test error";

        // Act
        await repository.SaveAsync(saga);

        // Assert
        var retrievedSaga = await repository.GetByIdAsync<TestSaga>(saga.Id);
        retrievedSaga.ShouldNotBeNull();
        var step = retrievedSaga.Steps[0];

        step.Status.ShouldBe(SagaStepStatus.Completed);
        step.IsPreValidation.ShouldBeTrue();
        step.HasCompensation.ShouldBeFalse();
        step.CompensationName.ShouldBe("UndoStep1");
        step.StartedAt.ShouldNotBeNull();
        step.CompletedAt.ShouldNotBeNull();
        step.ErrorMessage.ShouldBe("Test error");
    }

    [Fact]
    public async Task SaveAsync_ComplexDataTypes_SavesAndRetrievesCorrectly()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var saga = new TestSaga();

        // Store various data types
        var guidValue = Guid.NewGuid();
        saga.SetData("guid", guidValue);
        saga.SetData("string", "test string");
        saga.SetData("int", 42);
        saga.SetData("decimal", 99.99m);
        saga.SetData("bool", true);
        saga.SetData("datetime", DateTime.UtcNow);

        // Act
        await repository.SaveAsync(saga);
        var retrievedSaga = await repository.GetByIdAsync<TestSaga>(saga.Id);

        // Assert
        retrievedSaga.ShouldNotBeNull();
        retrievedSaga.GetData<Guid>("guid").ShouldBe(guidValue);
        retrievedSaga.GetData<string>("string").ShouldBe("test string");
        retrievedSaga.GetData<int>("int").ShouldBe(42);
        retrievedSaga.GetData<decimal>("decimal").ShouldBe(99.99m);
        retrievedSaga.GetData<bool>("bool").ShouldBeTrue();
        retrievedSaga.GetData<DateTime>("datetime").ShouldBeOfType<DateTime>();
    }

    // UpdateAsync tests
    [Fact]
    public async Task UpdateAsync_ExistingSaga_UpdatesSagaState()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var saga = new TestSaga();
        saga.MarkAsRunning();

        await repository.SaveAsync(saga);

        // Act
        saga.MarkStepAsCompleted("Step1");
        saga.SetData("newKey", "newValue");
        await repository.UpdateAsync(saga);

        // Assert
        var updatedSaga = await repository.GetByIdAsync<TestSaga>(saga.Id);
        updatedSaga.ShouldNotBeNull();
        updatedSaga.Steps.First(s => s.Name == "Step1").Status.ShouldBe(SagaStepStatus.Completed);
        updatedSaga.GetData<string>("newKey").ShouldBe("newValue");
    }

    [Fact]
    public async Task UpdateAsync_SagaNotFound_ThrowsException()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var saga = new TestSaga();
        saga.MarkAsRunning();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => repository.UpdateAsync(saga));
    }

    [Fact]
    public async Task UpdateAsync_PreservesSagaIdentity()
    {
        // Arrange
        var (repository, _) = await GetRepositoryAsync();
        var saga = new TestSaga();
        var originalId = saga.Id;
        var originalCreatedAt = saga.CreatedAt;

        await repository.SaveAsync(saga);

        // Act
        saga.MarkAsCompleted();
        await repository.UpdateAsync(saga);

        // Assert
        var updatedSaga = await repository.GetByIdAsync<TestSaga>(saga.Id);
        updatedSaga.ShouldNotBeNull();
        updatedSaga.Id.ShouldBe(originalId);
        updatedSaga.CreatedAt.ShouldBe(originalCreatedAt);
        updatedSaga.Status.ShouldBe(SagaStatus.Completed);
    }

    private class TestSaga : SagaBase
    {
        public TestSaga()
        {
            Steps = new List<SagaStep>
            {
                new() { Name = "Step1", Status = SagaStepStatus.Pending },
                new() { Name = "Step2", Status = SagaStepStatus.Pending }
            };
        }

        public void SetUserId(Guid userId)
        {
            SetData("userId", userId);
        }

        public Guid GetUserId()
        {
            return GetData<Guid>("userId");
        }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<SagaEntity> Sagas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SagaType).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.SagaData).IsRequired();
                entity.Property(e => e.Steps).IsRequired();
            });
        }
    }
}