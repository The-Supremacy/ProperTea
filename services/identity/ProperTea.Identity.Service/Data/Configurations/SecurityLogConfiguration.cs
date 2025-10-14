using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Data.Configurations;

public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Details).HasColumnType("jsonb");
    }
}