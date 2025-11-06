using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProperTea.Identity.Kernel.Models;

namespace ProperTea.Identity.Kernel.Data.Configurations;

public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Details).HasColumnType("jsonb");
    }
}