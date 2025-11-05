using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ProperTea.ProperSagas.Ef;

/// <summary>
///     Entity Framework Core implementation of ISagaRepository
/// </summary>
public class EfSagaRepository<TContext> : ISagaRepository where TContext : DbContext
{
    private readonly TContext _context;

    public EfSagaRepository(TContext context)
    {
        _context = context;
    }

    public async Task<TSaga?> GetByIdAsync<TSaga>(Guid sagaId) where TSaga : SagaBase
    {
        var entity = await GetSagasDbSet().FindAsync(sagaId);
        if (entity == null)
            return null;

        return MapToSaga<TSaga>(entity);
    }

    public async Task SaveAsync<TSaga>(TSaga saga) where TSaga : SagaBase
    {
        var entity = new SagaEntity
        {
            Id = saga.Id,
            SagaType = saga.SagaType,
            Status = saga.Status.ToString(),
            SagaData = saga.SagaData,
            Steps = JsonSerializer.Serialize(saga.Steps),
            ErrorMessage = saga.ErrorMessage,
            CreatedAt = saga.CreatedAt,
            CompletedAt = saga.CompletedAt
        };

        GetSagasDbSet().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync<TSaga>(TSaga saga) where TSaga : SagaBase
    {
        var entity = await GetSagasDbSet().FindAsync(saga.Id);
        if (entity == null)
            throw new InvalidOperationException($"Saga {saga.Id} not found");

        entity.Status = saga.Status.ToString();
        entity.SagaData = saga.SagaData;
        entity.Steps = JsonSerializer.Serialize(saga.Steps);
        entity.ErrorMessage = saga.ErrorMessage;
        entity.CompletedAt = saga.CompletedAt;

        await _context.SaveChangesAsync();
    }

    public async Task<List<Guid>> FindByStatusAsync(SagaStatus status)
    {
        return await GetSagasDbSet()
            .Where(s => s.Status == status.ToString())
            .Select(s => s.Id)
            .ToListAsync();
    }

    private DbSet<SagaEntity> GetSagasDbSet()
    {
        // Use reflection to get the Sagas DbSet from the context
        var property = _context.GetType().GetProperty("Sagas");
        if (property == null)
            throw new InvalidOperationException(
                $"DbContext {_context.GetType().Name} must have a DbSet<SagaEntity> property named 'Sagas'");

        return (DbSet<SagaEntity>)property.GetValue(_context)!;
    }

    private TSaga MapToSaga<TSaga>(SagaEntity entity) where TSaga : SagaBase
    {
        var saga = Activator.CreateInstance<TSaga>();

        // Use reflection to set protected properties
        var sagaType = typeof(SagaBase);
        sagaType.GetProperty(nameof(SagaBase.Id))!.SetValue(saga, entity.Id);
        sagaType.GetProperty(nameof(SagaBase.SagaType))!.SetValue(saga, entity.SagaType);
        sagaType.GetProperty(nameof(SagaBase.Status))!.SetValue(saga, Enum.Parse<SagaStatus>(entity.Status));
        sagaType.GetProperty(nameof(SagaBase.SagaData))!.SetValue(saga, entity.SagaData);
        sagaType.GetProperty(nameof(SagaBase.Steps))!.SetValue(saga,
            JsonSerializer.Deserialize<List<SagaStep>>(entity.Steps) ?? new List<SagaStep>());
        sagaType.GetProperty(nameof(SagaBase.ErrorMessage))!.SetValue(saga, entity.ErrorMessage);
        sagaType.GetProperty(nameof(SagaBase.CreatedAt))!.SetValue(saga, entity.CreatedAt);
        sagaType.GetProperty(nameof(SagaBase.CompletedAt))!.SetValue(saga, entity.CompletedAt);

        return saga;
    }
}