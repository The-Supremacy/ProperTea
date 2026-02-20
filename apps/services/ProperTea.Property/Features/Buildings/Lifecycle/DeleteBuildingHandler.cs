using JasperFx.Events;
using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record DeleteBuilding(Guid BuildingId);

public class DeleteBuildingHandler : IWolverineHandler
{
    public async Task Handle(DeleteBuilding command, IDocumentSession session)
    {
        var building = await session.Events.AggregateStreamAsync<BuildingAggregate>(command.BuildingId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                nameof(BuildingAggregate),
                command.BuildingId);

        var deleted = building.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.BuildingId, deleted, new Archived("Building deleted"));
    }
}
