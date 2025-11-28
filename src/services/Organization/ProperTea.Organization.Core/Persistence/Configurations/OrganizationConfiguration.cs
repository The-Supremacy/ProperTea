using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProperTea.Organization.Core.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Core.Organization>
{
    public void Configure(EntityTypeBuilder<Core.Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);

        builder.Property(o => o.Alias).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.Alias).IsUnique();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
    }
}