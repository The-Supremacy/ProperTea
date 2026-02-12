using Marten.Metadata;

namespace ProperTea.Property.Features.Companies;

public class CompanyReference : ITenanted
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public string? TenantId { get; set; }
}
