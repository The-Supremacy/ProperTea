using Microsoft.AspNetCore.Identity;

namespace ProperTea.Identity.Service.Models;

public class ProperTeaUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}