using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.RegisterOrganization;

public record RegisterOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string Slug,
    string CreatorUserId,
    string? EmailDomain);

public class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        _ = RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required")
            .MinimumLength(3).WithMessage("Organization name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Organization name cannot exceed 100 characters");

        _ = RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50).WithMessage("Slug cannot exceed 50 characters")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        _ = RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("Creator user ID is required");

        _ = When(x => !string.IsNullOrWhiteSpace(x.EmailDomain), () =>
        {
            _ = RuleFor(x => x.EmailDomain)
                .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9]\.[a-zA-Z]{2,}$")
                .WithMessage("Email domain must be a valid domain name (e.g., example.com)");
        });
    }
}

public record RegistrationResult(
    Guid OrganizationId,
    bool IsSuccess,
    string? Reason);

public static class RegisterOrganizationHandler
{
    public static async Task<RegistrationResult> Handle(
        RegisterOrganizationCommand command,
        IDocumentSession session,
        IZitadelClient zitadelClient,
        IMessageBus messageBus,
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

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationRegistered(
            command.OrganizationId,
            command.Name,
            command.Slug,
            zitadelOrgId,
            command.EmailDomain,
            DateTimeOffset.UtcNow
        );
        await messageBus.PublishAsync(integrationEvent);

        return new RegistrationResult(command.OrganizationId, IsSuccess: true, Reason: null);
    }
}
