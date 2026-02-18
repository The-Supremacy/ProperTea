using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record SelectBuildings(Guid PropertyId);

public record BuildingSelectItem(Guid Id, string Code, string Name);

public class SelectBuildingsHandler : IWolverineHandler
{
    public async Task<List<BuildingSelectItem>> Handle(SelectBuildings query, IDocumentSession session)
    {
        var buildings = await session.Query<BuildingAggregate>()
            .Where(b => b.PropertyId == query.PropertyId && b.CurrentStatus == BuildingAggregate.Status.Active)
            .OrderBy(b => b.Code)
            .Select(b => new BuildingSelectItem(b.Id, b.Code, b.Name))
            .ToListAsync();

        return [.. buildings];
    }
}
