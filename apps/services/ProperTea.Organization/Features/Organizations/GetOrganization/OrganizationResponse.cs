namespace ProperTea.Organization.Features.Organizations.GetOrganization;

public record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    OrganizationAggregate.Status Status,
    string? ZitadelOrganizationId,
    DateTimeOffset CreatedAt,
    int Version)
{
    public static OrganizationResponse FromAggregate(OrganizationAggregate aggregate)
    {
        return new OrganizationResponse(
            aggregate.Id,
            aggregate.Name,
            aggregate.Slug,
            aggregate.CurrentStatus,
            aggregate.ZitadelOrganizationId,
            aggregate.CreatedAt,
            aggregate.Version
        );
    }
}
