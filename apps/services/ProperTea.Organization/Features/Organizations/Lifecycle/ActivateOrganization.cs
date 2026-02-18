using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Organization.Features.Organizations.Projections;
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
        var orgView = await session.Query<OrganizationDetailsView>()
            .FirstOrDefaultAsync(o => o.OrganizationId == command.OrganizationId)
            ?? throw new NotFoundException(
                OrganizationErrorCodes.NOT_FOUND,
                nameof(OrganizationAggregate),
                command.OrganizationId);

        var org =
            await session.Events.AggregateStreamAsync<OrganizationAggregate>(orgView.Id)
            ?? throw new NotFoundException(
                OrganizationErrorCodes.NOT_FOUND,
                nameof(OrganizationAggregate),
                command.OrganizationId);

        var activated = org.Activate();

        _ = session.Events.Append(orgView.Id, activated);
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
    string OrganizationId,
    CancellationToken CancellationToken = default);
