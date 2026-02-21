using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record UpdateEntrance(Guid BuildingId, Guid EntranceId, string Code, string Name);

public class UpdateEntranceHandler : IWolverineHandler
{
    public async Task Handle(UpdateEntrance command, IDocumentSession session)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                "Building",
                command.BuildingId);

        var entranceUpdated = building.UpdateEntrance(command.EntranceId, command.Code, command.Name);

        _ = session.Events.Append(command.BuildingId, entranceUpdated);
        await session.SaveChangesAsync();
    }
}
