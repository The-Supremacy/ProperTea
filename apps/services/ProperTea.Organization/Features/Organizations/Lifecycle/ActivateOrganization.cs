using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public class ActivateHandler : IWolverineHandler
{
    public async Task Handle(
        ActivateCommand command,
        IDocumentSession session,
        IMessageBus messageBus,
        ILogger logger)
    {
        var org =
            await session.Events.AggregateStreamAsync<OrganizationAggregate>(command.OrganizationId)
            ?? throw new NotFoundException(
                OrganizationErrorCodes.NOT_FOUND,
                nameof(OrganizationAggregate),
                command.OrganizationId);

        var activated = org.Activate();

        _ = session.Events.Append(command.OrganizationId, activated);
        await session.SaveChangesAsync(command.CancellationToken);

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationActivated(
                command.OrganizationId,
                DateTimeOffset.UtcNow);
        await messageBus.PublishAsync(integrationEvent);

        logger.LogInformation(
            "Activated organization: {OrgId}",
            command.OrganizationId);
    }
}

public record ActivateCommand(
    Guid OrganizationId,
    CancellationToken CancellationToken = default);
