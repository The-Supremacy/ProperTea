namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationMessages
{
    public record StartRegistration(
        Guid OrganizationId,
        string Name,
        string Slug);

    public record CreateOrganization(
        Guid OrganizationId,
        string Name,
        string Slug);

    public record ActivateOrganization(
        Guid OrganizationId);

    public record RegistrationResult(
        Guid OrganizationId,
        string Name,
        string Slug,
        string Status,
        string? Reason);
}
