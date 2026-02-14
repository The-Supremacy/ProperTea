using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record DeleteUnit(Guid UnitId);

public class DeleteUnitHandler : IWolverineHandler
{
    public async Task Handle(
        DeleteUnit command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var unit = await session.Events.AggregateStreamAsync<UnitAggregate>(command.UnitId)
            ?? throw new NotFoundException(
                UnitErrorCodes.UNIT_NOT_FOUND,
                "Unit",
                command.UnitId);

        var deleted = unit.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.UnitId, deleted);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new UnitIntegrationEvents.UnitDeleted
        {
            UnitId = command.UnitId,
            PropertyId = unit.PropertyId,
            OrganizationId = organizationId,
            DeletedAt = deleted.DeletedAt
        });
    }
}
