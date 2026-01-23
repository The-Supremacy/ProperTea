using FluentValidation;
using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record UpdateIdentityCommand(
    Guid OrganizationId,
    string? NewName,
    string? NewSlug,
    CancellationToken CancellationToken = default);

public class UpdateIdentityValidator : AbstractValidator<UpdateIdentityCommand>
{
    public UpdateIdentityValidator()
    {
        _ = When(x => x.NewName != null, () => RuleFor(x => x.NewName).MinimumLength(3));
        _ = When(x => x.NewSlug != null, () => RuleFor(x => x.NewSlug).Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$"));
    }
}

public class UpdateIdentityHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateIdentityCommand command,
        IDocumentSession session,
        IExternalOrganizationClient externalOrganizationClient,
        IMessageBus messageBus,
        ILogger logger)
    {
        var org = await session.Events.AggregateStreamAsync<OrganizationAggregate>(command.OrganizationId)
            ?? throw new NotFoundException(nameof(OrganizationAggregate), command.OrganizationId);

        // 1. Uniqueness Checks
        if (command.NewName != null && command.NewName != org.Name)
        {
            if (await session.Query<OrganizationAggregate>().AnyAsync(x => x.Name == command.NewName && x.Id != command.OrganizationId))
                throw new ConflictException($"Name '{command.NewName}' taken");
        }
        if (command.NewSlug != null && command.NewSlug != org.Slug)
        {
            if (await session.Query<OrganizationAggregate>().AnyAsync(x => x.Slug == command.NewSlug && x.Id != command.OrganizationId))
                throw new ConflictException($"Slug '{command.NewSlug}' taken");
        }

        if (org.ExternalOrganizationId != null)
        {
            await externalOrganizationClient.UpdateOrganizationAsync(
                org.ExternalOrganizationId,
                command.NewName ?? org.Name,
                command.CancellationToken);
        }

        var events = new List<object>();

        if (command.NewName != null && command.NewName != org.Name)
            events.Add(org.Rename(command.NewName));

        if (command.NewSlug != null && command.NewSlug != org.Slug)
            events.Add(org.ChangeSlug(command.NewSlug));

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.OrganizationId, [.. events]);
            await session.SaveChangesAsync(command.CancellationToken);

            logger.LogInformation(
                "Updated organization identity: {OrgId} ({EventCount} events)",
                command.OrganizationId,
                events.Count);

            var integrationEvent = new OrganizationIntegrationEvents.OrganizationIdentityUpdated(
                command.OrganizationId,
                command.NewName ?? org.Name,
                command.NewSlug ?? org.Slug,
                DateTimeOffset.UtcNow);
            await messageBus.PublishAsync(integrationEvent);
        }
    }
}
