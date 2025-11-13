using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public class EfSagaRepository<TContext>(TContext context) : ISagaRepository
    where TContext : DbContext, ISagaDbContext
{
    public async Task<Saga?> GetByIdAsync(Guid sagaId)
    {
        var entity = await context.Sagas.FindAsync(sagaId);
        return entity == null ? null : MapToSaga(entity);
    }

    public async Task AddAsync(Saga saga)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var entity = MapToEntity(saga);
            context.Sagas.Add(entity);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TryUpdateAsync(Saga saga)
    {
        var entity = await context.Sagas.FindAsync(saga.Id);
        if (entity == null)
            return false;

        if (entity.Version != saga.Version)
            return false;

        saga.Version++;
        UpdateEntity(entity, saga);

        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            saga.Version--;
            return false;
        }
    }

    public async Task<List<Saga>> FindSagasNeedingResumptionAsync(TimeSpan lockTimeout)
    {
        var staleTime = DateTime.UtcNow.Add(-lockTimeout);

        return (await context.Sagas
                .Where(s => (s.Status == SagaStatus.Running || s.Status == SagaStatus.WaitingForCallback) &&
                            (!s.LockedAt.HasValue || s.LockedAt.Value < staleTime))
                .ToListAsync())
            .Select(MapToSaga).ToList();
    }

    public async Task<List<Saga>> FindTimedOutSagasAsync()
    {
        var now = DateTime.UtcNow;

        return (await context.Sagas
                .Where(s => s.Status == SagaStatus.Running &&
                            s.TimeoutDeadline.HasValue &&
                            s.TimeoutDeadline.Value < now)
                .ToListAsync())
            .Select(MapToSaga).ToList();
    }

    public async Task<List<Saga>> FindFailedSagasAsync()
    {
        var entities = await context.Sagas
            .Where(s => s.Status == SagaStatus.Failed || s.Status == SagaStatus.FailedAfterPonr)
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .ToListAsync();

        return entities.Select(MapToSaga).ToList();
    }

    public async Task<List<Saga>> FindSagasByCorrelationIdAsync(string correlationId)
    {
        var entities = await context.Sagas
            .Where(s => s.CorrelationId == correlationId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .ToListAsync();

        return entities.Select(MapToSaga).ToList();
    }

    public async Task<List<Saga>> FindScheduledSagasAsync(DateTime asOf)
    {
        var entities = await context.Sagas
            .Where(s => s.Status == SagaStatus.Scheduled 
                        && (!s.ScheduledFor.HasValue || s.ScheduledFor <= asOf))
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .ToListAsync();

        return entities.Select(MapToSaga).ToList();
    }

    private static void UpdateEntity(SagaEntity entity, Saga saga)
    {
        entity.Status = saga.Status;
        entity.Version = saga.Version;
        entity.LockToken = saga.LockToken;
        entity.LockedAt = saga.LockedAt;
        entity.SagaData = saga.SagaData;
        entity.Steps = JsonSerializer.Serialize(saga.Steps);
        entity.ErrorMessage = saga.ErrorMessage;
        entity.CompletedAt = saga.CompletedAt;
        entity.IsCancellationRequested = saga.IsCancellationRequested;
        entity.CancellationRequestedAt = saga.CancellationRequestedAt;
        entity.TimeoutDeadline = saga.TimeoutDeadline;
        entity.ScheduledFor = saga.ScheduledFor;
        entity.UpdatedAt = saga.UpdatedAt;
    }

    private static SagaEntity MapToEntity(Saga saga)
    {
        return new SagaEntity
        {
            Id = saga.Id,
            SagaType = saga.SagaType,
            CreatedAt = saga.CreatedAt,
            CorrelationId = saga.CorrelationId,
            TraceId = saga.TraceId,
            Status = saga.Status,
            Version = saga.Version,
            LockToken = saga.LockToken,
            LockedAt = saga.LockedAt,
            SagaData = saga.SagaData,
            Steps = JsonSerializer.Serialize(saga.Steps),
            ErrorMessage = saga.ErrorMessage,
            CompletedAt = saga.CompletedAt,
            IsCancellationRequested = saga.IsCancellationRequested,
            CancellationRequestedAt = saga.CancellationRequestedAt,
            TimeoutDeadline = saga.TimeoutDeadline,
            ScheduledFor = saga.ScheduledFor,
            UpdatedAt = saga.UpdatedAt,
        };
    }

    private static Saga MapToSaga(SagaEntity entity)
    {
        TimeSpan? timeout = entity.TimeoutDeadline.HasValue
            ? entity.TimeoutDeadline.Value > DateTime.UtcNow
                ? entity.TimeoutDeadline.Value - DateTime.UtcNow
                : TimeSpan.Zero
            : null;

        return new Saga
        {
            Id = entity.Id,
            SagaType = entity.SagaType,
            CreatedAt = entity.CreatedAt,
            CorrelationId = entity.CorrelationId,
            TraceId = entity.TraceId,
            Timeout = timeout,
            Status = entity.Status,
            Version = entity.Version,
            LockToken = entity.LockToken,
            LockedAt = entity.LockedAt,
            SagaData = entity.SagaData,
            Steps = JsonSerializer.Deserialize<List<SagaStep>>(entity.Steps) ?? [],
            ErrorMessage = entity.ErrorMessage,
            CompletedAt = entity.CompletedAt,
            IsCancellationRequested = entity.IsCancellationRequested,
            CancellationRequestedAt = entity.CancellationRequestedAt,
            TimeoutDeadline = entity.TimeoutDeadline,
            ScheduledFor = entity.ScheduledFor,
            UpdatedAt = entity.UpdatedAt
        };
    }
}