using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;
using ProperTea.ServiceDefaults.Auth;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record RegisterOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string Alias,
    string Slug);

public class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        _ = RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        _ = RuleFor(x => x.Alias).NotEmpty().Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }
}

public record RegistrationResult(Guid OrganizationId, bool IsSuccess, string? Reason);

public class RegisterOrganizationHandler : IWolverineHandler
{
    public async Task<(OrganizationEvents.OrganizationRegistered, RegistrationResult)> Handle(
        RegisterOrganizationCommand command,
        IDocumentSession session,
        IExternalOrganizationClient externalOrgClient,
        IUserContext userContext,
        IMessageBus messageBus,
        ILogger logger)
    {
        var userId = userContext.UserId
            ?? throw new UnauthorizedAccessException("User must be logged in to register an organization");

        var exists = await session.Query<OrganizationAggregate>()
            .AnyAsync(x => x.Slug == command.Alias || x.Name == command.Name);

        if (exists)
            throw new ConflictException($"Organization with alias '{command.Alias}' or name '{command.Name}' already exists");

        logger.LogInformation("Provisioning org {OrganizationId}", command.OrganizationId);

        var externalOrgId = await externalOrgClient.CreateOrganizationAsync(
            command.Name,
            CancellationToken.None);

        await externalOrgClient.AddUserToOrganizationAsync(
            externalOrgId,
            userId,
            [],
            CancellationToken.None);

        var events = new List<object>
        {
            OrganizationAggregate.Create(command.OrganizationId, command.Name, command.Slug),
            OrganizationAggregate.LinkExternalOrganization(command.OrganizationId, externalOrgId),
            new OrganizationEvents.Activated(command.OrganizationId, DateTime.UtcNow)
        };

        _ = session.Events.StartStream<OrganizationAggregate>(command.OrganizationId, [.. events]);
        await session.SaveChangesAsync();

        await messageBus.PublishAsync(new OrganizationIntegrationEvents.OrganizationRegistered(
            command.OrganizationId,
            command.Name,
            command.Slug,
            externalOrgId,
            DateTimeOffset.UtcNow
        ));

        return (
            new OrganizationEvents.OrganizationRegistered(command.OrganizationId),
            new RegistrationResult(command.OrganizationId, IsSuccess: true, Reason: null)
        );
    }
}
