using ProperTea.Organization.Persistence;
using ProperTea.Organization.Utility;
using Wolverine.Persistence;

namespace ProperTea.Organization.Features.Organizations.Create;

public record CreateKeycloakOrganization(Guid OrganizationId, Guid CreatorUserId);
public record KeycloakOrganizationCreated(Guid OrganizationId);
public record KeycloakOrganizationCreationFailed(Guid OrganizationId, string Reason);

public static class CreateKeycloakOrganizationHandler
{
    public static async Task<object> HandleAsync(
        CreateKeycloakOrganization command,
        [Entity] Domain.Organization organization,
        IKeycloakAdminClient keycloakClient)
    {
        try
        {
            await keycloakClient.CreateOrganizationAsync(
                command.OrganizationId,
                organization.Name,
                organization.OrgAlias).ConfigureAwait(false);
            return new KeycloakOrganizationCreated(command.OrganizationId);
        }
        catch (Exception ex)
        {
            return new KeycloakOrganizationCreationFailed(command.OrganizationId, ex.Message);
        }
    }
}

public record RemoveLocalOrganization(Guid Id);
public static class RemoveLocalOrganizationHandler
{
    // This is an INTERNAL ONLY handler. It has no publishing rule.
    public static async Task HandleAsync(
        RemoveLocalOrganization command,
        OrganizationDbContext dbContext)
    {
        var organization = await dbContext.Organizations.FindAsync(command.Id).ConfigureAwait(false);
        if (organization != null)
        {
            // Idempotent.
            dbContext.Organizations.Remove(organization);
        }
    }
}

public record AddUserToKeycloakOrganization(Guid UserId, Guid OrganizationId);
public record UserAddedToKeycloakOrganization(Guid UserId, Guid OrganizationId);
public record AddUserToKeycloakOrganizationFailed(Guid UserId, Guid OrganizationId, string Message);

public static class AddUserToKeycloakOrganizationHandler
{
    public static async Task<object> HandleAsync(
        AddUserToKeycloakOrganization command,
        IKeycloakAdminClient keycloakClient)
    {
        try
        {
            await keycloakClient.AddUserToOrganizationAsync(command.UserId, command.OrganizationId).ConfigureAwait(false);
            return new UserAddedToKeycloakOrganization(command.UserId, command.OrganizationId);
        }
        catch (Exception ex)
        {
            // For now - just notification.
            return new AddUserToKeycloakOrganizationFailed(command.UserId, command.OrganizationId, ex.Message);
        }
    }
}
