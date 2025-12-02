using ProperTea.Utilities;

namespace ProperTea.Organization.Domain;

public class Organization
{
    private Organization() {}

    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Alias { get; set; }
    public string? LogoUrl { get; set; }
    public required OrganizationStatus Status { get; set; }
    public string? ExternalIdentityId { get; set; }

    public static Organization Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
        {
            throw new InvalidOperationException("Organization name must be at least 3 characters long.");
        }

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Alias = SlugGenerator.Generate(name),
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
