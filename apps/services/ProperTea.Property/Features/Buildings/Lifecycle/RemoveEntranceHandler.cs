using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Units;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record RemoveEntrance(Guid BuildingId, Guid EntranceId);

public class RemoveEntranceHandler : IWolverineHandler
{
    public async Task Handle(RemoveEntrance command, IDocumentSession session)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                "Building",
                command.BuildingId);

        var referencingUnits = await session.Query<UnitAggregate>()
            .Where(u => u.EntranceId == command.EntranceId
                && u.CurrentStatus == UnitAggregate.Status.Active)
            .CountAsync();

        if (referencingUnits > 0)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_ENTRANCE_HAS_ACTIVE_UNITS,
                $"Cannot remove entrance because {referencingUnits} active unit(s) reference it");

        var entranceRemoved = building.RemoveEntrance(command.EntranceId);

        _ = session.Events.Append(command.BuildingId, entranceRemoved);
        await session.SaveChangesAsync();
    }
}
