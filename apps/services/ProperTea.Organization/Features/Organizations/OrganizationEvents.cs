namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEvents
{
    public record Created(
        Guid OrganizationId,
        OrganizationAggregate.SubscriptionTier Tier,
        DateTimeOffset CreatedAt);

    public record ExternalOrganizationCreated(
        Guid OrganizationId,
        string ExternalOrganizationId);

    public record Activated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt);

    public record OrganizationRegistered(
        Guid OrganizationId);
}
