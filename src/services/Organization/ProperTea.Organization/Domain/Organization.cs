namespace ProperTea.Organization.Core;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Alias { get; set; }
    public string? LogoUrl { get; set; }
    public required OrganizationStatus Status { get; set; }
    public string? ExternalIdentityId { get; set; }
}

public enum OrganizationStatus
{
    Pending = 0,
    Inactive = 1,
    Active = 2
}
