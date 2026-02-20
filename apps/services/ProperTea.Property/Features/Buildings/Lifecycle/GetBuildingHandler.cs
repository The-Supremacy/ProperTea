using Marten;
using ProperTea.Infrastructure.Common.Address;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record GetBuilding(Guid BuildingId);

public record EntranceResponse(Guid Id, string Code, string Name);

public record BuildingResponse(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    Address Address,
    IReadOnlyList<EntranceResponse> Entrances,
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
            building.Address,
            [.. building.Entrances.Select(e => new EntranceResponse(e.Id, e.Code, e.Name))],
            building.CurrentStatus.ToString(),
            building.CreatedAt);
    }
}
