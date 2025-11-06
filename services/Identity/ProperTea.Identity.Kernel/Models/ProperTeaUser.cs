using Microsoft.AspNetCore.Identity;

namespace ProperTea.Identity.Kernel.Models;

public class ProperTeaUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}