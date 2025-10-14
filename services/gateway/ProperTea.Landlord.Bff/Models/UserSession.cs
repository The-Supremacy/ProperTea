namespace ProperTea.Landlord.Bff.Models;

public class UserSession
{
    public required string SessionId { get; set; }
    public required string UserId { get; set; }

    public string? OrgId { get; set; }
    public required string EnrichedJwt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastRefreshedAt { get; set; }
    public DeviceInfo? DeviceInfo { get; set; }
}

public class DeviceInfo
{
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}