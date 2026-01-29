using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.Infrastructure.Common.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record RegisterOrganizationCommand(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string UserPassword);

public class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        _ = RuleFor(x => x.OrganizationName).NotEmpty().MinimumLength(2).MaximumLength(100);
        _ = RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
        _ = RuleFor(x => x.UserFirstName).NotEmpty().MinimumLength(1).MaximumLength(100);
        _ = RuleFor(x => x.UserLastName).NotEmpty().MinimumLength(1).MaximumLength(100);
        _ = RuleFor(x => x.UserPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .MaximumLength(100)
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one number")
            .Matches(@"[^A-Za-z0-9]").WithMessage("Password must contain at least one special character");
    }
}

public record RegistrationResult(Guid OrganizationId, bool IsSuccess, string? Reason);

public class RegisterOrganizationHandler : IWolverineHandler
{
    public async Task<(OrganizationEvents.OrganizationRegistered, RegistrationResult, OrganizationIntegrationEvents.OrganizationRegistered)> Handle(
        RegisterOrganizationCommand command,
        IDocumentSession session,
        IExternalOrganizationClient externalOrgClient,
        ILogger logger,
        CancellationToken ct)
    {
        var exists = await externalOrgClient.CheckOrganizationExistsAsync(command.OrganizationName, ct);
        if (exists)
            throw new ConflictException($"Organization with name '{command.OrganizationName}' already exists");

        var externalOrgId = await externalOrgClient.CreateOrganizationWithAdminAsync(
            command.OrganizationName,
            command.UserEmail,
            command.UserFirstName,
            command.UserLastName,
            command.UserPassword,
            ct);

        var orgId = Guid.NewGuid();
        var events = new List<object>
        {
            OrganizationAggregate.Create(orgId),
            OrganizationAggregate.LinkExternalOrganization(orgId, externalOrgId),
            new OrganizationEvents.Activated(orgId, DateTime.UtcNow)
        };
        _ = session.Events.StartStream<OrganizationAggregate>(orgId, [.. events]);
        await session.SaveChangesAsync(ct);

        logger.LogInformation("Registered new organization {OrganizationId} '{Name}' with external ID {ExternalOrgId}",
            orgId,
            command.OrganizationName,
            externalOrgId);

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationRegistered(
            orgId,
            command.OrganizationName,
            externalOrgId,
            DateTimeOffset.UtcNow
        );

        return (
            new OrganizationEvents.OrganizationRegistered(orgId),
            new RegistrationResult(orgId, IsSuccess: true, Reason: null),
            integrationEvent
        );
    }
}
