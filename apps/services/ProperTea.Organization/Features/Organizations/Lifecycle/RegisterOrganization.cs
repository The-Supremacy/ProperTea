using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;
using ProperTea.ServiceDefaults.Auth;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record RegisterOrganizationCommand(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string Slug);

public class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        _ = RuleFor(x => x.OrganizationName).NotEmpty().MinimumLength(2).MaximumLength(100);
        _ = RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
        _ = RuleFor(x => x.UserFirstName).NotEmpty().MinimumLength(1).MaximumLength(100);
        _ = RuleFor(x => x.UserLastName).NotEmpty().MinimumLength(1).MaximumLength(100);
        _ = RuleFor(x => x.Slug).NotEmpty().Matches(OrganizationAggregate.SlugPattern());
    }
}

public record RegistrationResult(Guid OrganizationId, bool IsSuccess, string? Reason);

public class RegisterOrganizationHandler : IWolverineHandler
{
    public async Task<(OrganizationEvents.OrganizationRegistered, RegistrationResult, OrganizationIntegrationEvents.OrganizationRegistered)> Handle(
        RegisterOrganizationCommand command,
        IDocumentSession session,
        IExternalOrganizationClient externalOrgClient,
        IUserContext userContext,
        ILogger logger)
    {
        var userId = userContext.UserId
            ?? throw new UnauthorizedAccessException("User must be logged in to register an organization");

        var exists = await session.Query<OrganizationAggregate>()
            .AnyAsync(x => x.Name == command.OrganizationName);

        if (exists)
            throw new ConflictException($"Organization with name '{command.OrganizationName}' already exists");

        var externalOrgId = await externalOrgClient.CreateOrganizationWithAdminAsync(
            command.OrganizationName,
            command.UserEmail,
            command.UserFirstName,
            command.UserLastName,
            CancellationToken.None);

        var orgId = Guid.NewGuid();
        var events = new List<object>
        {
            OrganizationAggregate.Create(orgId, command.OrganizationName, command.Slug),
            OrganizationAggregate.LinkExternalOrganization(orgId, externalOrgId),
            new OrganizationEvents.Activated(orgId, DateTime.UtcNow)
        };
        _ = session.Events.StartStream<OrganizationAggregate>(orgId, [.. events]);
        await session.SaveChangesAsync();

        logger.LogInformation("Registered new organization {OrganizationId} with external ID {ExternalOrgId}",
            orgId,
            externalOrgId);

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationRegistered(
            orgId,
            command.OrganizationName,
            command.Slug,
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
