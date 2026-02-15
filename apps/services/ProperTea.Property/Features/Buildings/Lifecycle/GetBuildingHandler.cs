using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record GetBuilding(Guid BuildingId);

public record BuildingResponse(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public class GetBuildingHandler : IWolverineHandler
{
    public async Task<BuildingResponse?> Handle(GetBuilding query, IDocumentSession session)
    {
        var building = await session.LoadAsync<BuildingAggregate>(query.BuildingId);

        if (building is null || building.CurrentStatus == BuildingAggregate.Status.Deleted)
            return null;

        return new BuildingResponse(
            building.Id,
            building.PropertyId,
            building.Code,
            building.Name,
            building.CurrentStatus.ToString(),
            building.CreatedAt);
    }
}
