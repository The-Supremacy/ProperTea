using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProperTea.Identity.Service.Models;

namespace ProperTea.Identity.Service.Data.Configurations;

public class ProperTeaUserConfiguration : IEntityTypeConfiguration<ProperTeaUser>
{
    public void Configure(EntityTypeBuilder<ProperTeaUser> builder)
    {
    }
}