using Microsoft.EntityFrameworkCore;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplySagaConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SagaEntityConfiguration());
        return modelBuilder;
    }
}