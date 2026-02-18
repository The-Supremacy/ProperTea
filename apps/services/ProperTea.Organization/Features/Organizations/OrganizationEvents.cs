namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEvents
{
    public record Created(
        Guid OrganizationId,
        OrganizationAggregate.SubscriptionTier Tier,
        DateTimeOffset CreatedAt);

    public record OrganizationLinked(
        Guid StreamId,
        string OrganizationId);

    public record Activated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt);

    public record OrganizationRegistered(
        Guid OrganizationId);
}
