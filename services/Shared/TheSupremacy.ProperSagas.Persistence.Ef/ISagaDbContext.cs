using Microsoft.EntityFrameworkCore;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public interface ISagaDbContext
{
    DbSet<SagaEntity> Sagas { get; }
}