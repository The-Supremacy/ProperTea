using Marten.Events.Aggregation;

namespace ProperTea.Organization.Features.Organizations.Projections;

public class OrganizationListView : SingleStreamProjection<OrganizationAggregate, Guid>
{
    public Guid Id { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    // TODO: Add more properties as needed (PropertyCount, UserCount, LastActivityDate, etc.)
}
