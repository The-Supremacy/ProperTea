using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationMessages;

namespace ProperTea.Organization.Features.Organizations.RegisterOrganization;

public static class RegisterOrganizationHandler
{
    public static async Task<RegistrationResult> Handle(
        StartRegistration command,
        IDocumentSession session,
        IZitadelClient zitadelClient,
        ILogger logger)
    {
        // 1. Validate uniqueness (application-level concern - requires query)
        var exists = await session.Query<OrganizationAggregate>()
            .AnyAsync(x => x.Slug == command.Slug || x.Name == command.Name);

        if (exists)
        {
            throw new ConflictException(
                $"Organization with slug '{command.Slug}' or name '{command.Name}' already exists");
        }

        logger.LogInformation(
            "Provisioning organization {OrganizationId} in ZITADEL",
            command.OrganizationId);

        var zitadelOrgId = await zitadelClient.CreateOrganizationAsync(
            command.Name,
            CancellationToken.None);

        logger.LogInformation(
            "Successfully provisioned ZITADEL org {ZitadelOrgId} for {OrganizationId}",
            zitadelOrgId,
            command.OrganizationId);

        // Add creator as ORG_OWNER
        await zitadelClient.AddUserToOrganizationAsync(
            zitadelOrgId,
            command.CreatorUserId,
            ["ORG_OWNER"],
            CancellationToken.None);

        logger.LogInformation(
            "Added user {UserId} as ORG_OWNER to organization {ZitadelOrgId}",
            command.CreatorUserId,
            zitadelOrgId);

        var events = new List<object>();

        var created = OrganizationAggregate.Create(
            command.OrganizationId,
            command.Name,
            command.Slug);
        events.Add(created);

        var zitadelLinked = OrganizationAggregate.LinkZitadel(
            command.OrganizationId,
            zitadelOrgId);
        events.Add(zitadelLinked);

        // Add domain if provided
        if (!string.IsNullOrWhiteSpace(command.EmailDomain))
        {
            await zitadelClient.AddOrgDomainAsync(
                zitadelOrgId,
                command.EmailDomain,
                CancellationToken.None);

            logger.LogInformation(
                "Added domain {Domain} to organization {ZitadelOrgId}",
                command.EmailDomain,
                zitadelOrgId);

            var domainAdded = OrganizationAggregate.AddDomain(
                command.OrganizationId,
                command.EmailDomain);
            events.Add(domainAdded);
        }

        var activated = OrganizationAggregate.Activate(command.OrganizationId);
        events.Add(activated);

        _ = session.Events.StartStream<OrganizationAggregate>(
            command.OrganizationId,
            [.. events]);

        await session.SaveChangesAsync();

        return new RegistrationResult(command.OrganizationId, IsSuccess: true, Reason: null);
    }
}
