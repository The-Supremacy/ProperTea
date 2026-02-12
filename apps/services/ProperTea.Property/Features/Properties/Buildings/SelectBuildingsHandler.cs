using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Buildings;

public record SelectBuildings(Guid PropertyId);

public record BuildingSelectItem(Guid Id, string Code, string Name);

public class SelectBuildingsHandler : IWolverineHandler
{
    public async Task<List<BuildingSelectItem>> Handle(
        SelectBuildings query,
        IDocumentSession session)
    {
        var property = await session.LoadAsync<PropertyAggregate>(query.PropertyId);

        if (property == null || property.CurrentStatus == PropertyAggregate.Status.Deleted)
        {
            throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                $"Property {query.PropertyId} not found");
        }

        return [.. property.Buildings
            .Where(b => !b.IsRemoved)
            .OrderBy(b => b.Code)
            .Select(b => new BuildingSelectItem(b.Id, b.Code, b.Name))];
    }
}
