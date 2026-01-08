namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEvents
{
    public record Created(
        Guid OrganizationId,
        string Name,
        string Slug,
        DateTimeOffset CreatedAt);

    public record ZitadelProvisioningSucceeded(
        Guid OrganizationId,
        string ZitadelOrganizationId);

    public record ZitadelProvisioningFailed(
        Guid OrganizationId,
        string Reason);

    public record Activated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt);

    public record ActivationFailed(
        Guid OrganizationId,
        string Reason);
}
