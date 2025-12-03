using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProperTea.Organization.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Domain.Organization>
{
    public void Configure(EntityTypeBuilder<Domain.Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);

        builder.Property(o => o.OrgAlias).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.OrgAlias).IsUnique();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
    }
}
