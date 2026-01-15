using Marten;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public static class ActivateHandler
{
    public static async Task Handle(
        ActivateCommand command,
        IDocumentSession session,
        ILogger logger)
    {
        var org =
            await session.Events.AggregateStreamAsync<OrganizationAggregate>(command.OrganizationId)
            ?? throw new NotFoundException(nameof(OrganizationAggregate), command.OrganizationId);

        var activated = org.Activate();

        _ = session.Events.Append(command.OrganizationId, activated);
        await session.SaveChangesAsync(command.CancellationToken);

        logger.LogInformation(
            "Activated organization: {OrgId}",
            command.OrganizationId);
    }
}

public record ActivateCommand(
    Guid OrganizationId,
    CancellationToken CancellationToken = default);
