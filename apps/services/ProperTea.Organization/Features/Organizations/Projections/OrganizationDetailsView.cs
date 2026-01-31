using Marten.Events.Aggregation;

namespace ProperTea.Organization.Features.Organizations.Projections;

public class OrganizationDetailsView : SingleStreamProjection<OrganizationAggregate, Guid>
{
    public Guid Id { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string? ExternalOrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }

    // TODO: Add denormalized data from related aggregates (TotalProperties, TotalUsers, etc.)
}
