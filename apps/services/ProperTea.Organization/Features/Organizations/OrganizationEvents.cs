// OrganizationEvents.cs
namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEvents
{
    public record Created(
        Guid OrganizationId,
        string Name,
        string Slug,
        DateTimeOffset CreatedAt);

    public record ExternalOrganizationCreated(
        Guid OrganizationId,
        string ExternalOrganizationId);

    public record Activated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt);

    public record OrganizationRegistered(
        Guid OrganizationId);

    public record NameChanged(
        Guid OrganizationId,
        string NewName,
        DateTimeOffset ChangedAt);

    public record SlugChanged(
        Guid OrganizationId,
        string NewSlug,
        DateTimeOffset ChangedAt);

    public record Deactivated(
        Guid OrganizationId,
        string Reason,
        DateTimeOffset DeactivatedAt);

    public record DomainAdded(
        Guid OrganizationId,
        string EmailDomain,
        DateTimeOffset AddedAt);

    public record DomainVerified(
        Guid OrganizationId,
        DateTimeOffset VerifiedAt);
}
