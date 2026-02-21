using JasperFx.Events;
using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Units;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record DeleteBuilding(Guid BuildingId);

public class DeleteBuildingHandler : IWolverineHandler
{
    public async Task Handle(
        DeleteBuilding command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                nameof(BuildingAggregate),
                command.BuildingId);

        // Block deletion if building has active units
        var unitCount = await session.Query<UnitAggregate>()
            .Where(u => u.BuildingId == command.BuildingId && u.CurrentStatus == UnitAggregate.Status.Active)
            .CountAsync();

        if (unitCount > 0)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_HAS_ACTIVE_UNITS,
                $"Cannot delete building because it has {unitCount} active unit(s). Remove all units first.",
                new Dictionary<string, object> { ["unitCount"] = unitCount });

        var deleted = building.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.BuildingId, deleted, new Archived("Building deleted"));
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new BuildingIntegrationEvents.BuildingDeleted
        {
            BuildingId = command.BuildingId,
            PropertyId = building.PropertyId,
            OrganizationId = organizationId,
            DeletedAt = deleted.DeletedAt
        });
    }
}
