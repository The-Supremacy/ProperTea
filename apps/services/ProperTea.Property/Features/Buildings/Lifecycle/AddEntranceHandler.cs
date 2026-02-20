using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record AddEntrance(Guid BuildingId, string Code, string Name);

public class AddEntranceHandler : IWolverineHandler
{
    public async Task<Guid> Handle(AddEntrance command, IDocumentSession session)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                "Building",
                command.BuildingId);

        var entranceAdded = building.AddEntrance(command.Code, command.Name);

        _ = session.Events.Append(command.BuildingId, entranceAdded);
        await session.SaveChangesAsync();

        return entranceAdded.EntranceId;
    }
}
