namespace ProperTea.Organization.Domain;

public class Organization
{
    private Organization() {}

    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string OrgAlias { get; set; }
    public string? LogoUrl { get; set; }
    public required OrganizationStatus Status { get; set; }
    public string? ExternalIdentityId { get; set; }

    public static Organization Create(string name, string orgAlias)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
        {
            throw new InvalidOperationException("Organization name must be at least 3 characters long.");
        }

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrgAlias = orgAlias,
            Status = OrganizationStatus.Pending
        };
    }
}

public enum OrganizationStatus
{
    Pending = 0,
    Inactive = 1,
    Active = 2
}
