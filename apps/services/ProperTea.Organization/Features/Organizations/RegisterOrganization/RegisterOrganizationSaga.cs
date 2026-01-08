using Marten;
using Wolverine;
using ProperTea.Organization.Infrastructure.Zitadel;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;
using static ProperTea.Organization.Features.Organizations.OrganizationMessages;
using Wolverine.Marten;

namespace ProperTea.Organization.Features.Organizations.RegisterOrganization;

public class RegisterOrganizationHandler
{
    public static async Task<StartRegisterOrganizationSaga> Handle(
        StartRegistration command,
        IQuerySession session)
    {
        var existingOrg = await session.Query<OrganizationAggregate>()
            .Where(x => x.Slug == command.Slug || x.Name == command.Name)
            .FirstOrDefaultAsync();

        if (existingOrg != null)
        {
            throw new InvalidOperationException(
                $"Organization with slug '{command.Slug}' or name '{command.Name}' already exists");
        }

        return new StartRegisterOrganizationSaga(
            command.OrganizationId,
            command.Name,
            command.Slug);
    }
}

public record StartRegisterOrganizationSaga(
        Guid OrganizationId,
        string Name,
        string Slug);

/// <summary>
/// Orchestrates complete organization registration workflow:
/// 1. Validate slug uniqueness
/// 2. Create organization aggregate
/// 3. Provision in ZITADEL
/// 4. Activate organization
/// Returns result to HTTP endpoint
/// </summary>
public class RegisterOrganizationSaga : Saga
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ZitadelOrganizationId { get; set; }
    public string? FailureReason { get; set; }

    public static RegisterOrganizationSaga Start(StartRegisterOrganizationSaga command)
    {
        return new RegisterOrganizationSaga
        {
            Id = command.OrganizationId,
            Name = command.Name,
            Slug = command.Slug
        };
    }

    [AggregateHandler]
    public async Task<Created> HandleAsync(CreateOrganization command)
    {
        return new Created(
            command.OrganizationId,
            command.Name,
            command.Slug,
            DateTimeOffset.UtcNow);
    }

    public async Task<object> HandleAsync(
        Created created,
        IZitadelClient zitadelClient,
        ILogger<RegisterOrganizationSaga> logger)
    {
        try
        {
            logger.LogInformation(
                "Provisioning organization {OrganizationId} in ZITADEL",
                created.OrganizationId);

            var zitadelOrgId = await zitadelClient.CreateOrganizationAsync(
                created.Name,
                CancellationToken.None);

            logger.LogInformation(
                "Successfully provisioned ZITADEL org {ZitadelOrgId}",
                zitadelOrgId);

            return new ZitadelProvisioningSucceeded(
                created.OrganizationId,
                zitadelOrgId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision in ZITADEL");

            return new ZitadelProvisioningFailed(
                created.OrganizationId,
                ex.Message);
        }
    }

    public ActivateOrganization Handle(ZitadelProvisioningSucceeded succeeded)
    {
        ZitadelOrganizationId = succeeded.ZitadelOrganizationId;

        return new ActivateOrganization(succeeded.OrganizationId);
    }

    [AggregateHandler]
    public Activated Handle(
        ActivateOrganization command,
        OrganizationAggregate aggregate)
    {
        if (aggregate.CurrentStatus == OrganizationAggregate.Status.Active)
        {
            throw new InvalidOperationException("Organization is already active");
        }

        if (string.IsNullOrEmpty(aggregate.ZitadelOrganizationId))
        {
            throw new InvalidOperationException(
                "Cannot activate organization without ZITADEL provisioning");
        }

        return new Activated(
            command.OrganizationId,
            DateTimeOffset.UtcNow);
    }

    public void Handle(ZitadelProvisioningFailed failed)
    {
        FailureReason = failed.Reason;
        MarkCompleted();
    }

    public RegistrationResult Handle(Activated activated)
    {
        MarkCompleted();

        return new RegistrationResult(
            activated.OrganizationId,
            Name,
            Slug,
            Status: "Active",
            Reason: null);
    }

    public RegistrationResult Handle(ActivationFailed failed)
    {
        FailureReason = failed.Reason;
        MarkCompleted();

        return new RegistrationResult(
            failed.OrganizationId,
            Name,
            Slug,
            Status: "ProvisioningFailed",
            Reason: failed.Reason);
    }
}
