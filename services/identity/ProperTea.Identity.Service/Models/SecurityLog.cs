namespace ProperTea.Identity.Service.Models;

public class SecurityLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Event { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}