using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public class SagaEntityConfiguration : IEntityTypeConfiguration<SagaEntity>
{
    public void Configure(EntityTypeBuilder<SagaEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SagaType)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(e => e.DisplayName)
            .HasMaxLength(255);

        builder.Property(e => e.Status)
            .HasConversion<string>() // Store as string in case enum changes
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.SagaData)
            .IsRequired();

        builder.Property(e => e.Steps)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CompletedAt);

        // Indexes for queries
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.SagaType);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasIndex(e => new { e.Status, e.LockedAt });
        builder.HasIndex(e => new { e.Status, e.TimeoutDeadline });
        builder.HasIndex(e => e.CorrelationId);

        builder.Property(e => e.Version)
            .IsRequired()
            .IsConcurrencyToken();
    }
}