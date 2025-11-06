using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProperTea.Identity.Kernel.Models;

namespace ProperTea.Identity.Kernel.Data.Configurations;

public class ProperTeaUserConfiguration : IEntityTypeConfiguration<ProperTeaUser>
{
    public void Configure(EntityTypeBuilder<ProperTeaUser> builder)
    {
    }
}