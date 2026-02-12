using Marten;
using ProperTea.Property.Features.Properties.Lifecycle;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Buildings;

public record ListBuildings(Guid PropertyId);

public class ListBuildingsHandler : IWolverineHandler
{
    public async Task<List<BuildingResponse>> Handle(
        ListBuildings query,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(query.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                query.PropertyId);

        return [.. property.Buildings
            .Where(b => !b.IsRemoved)
            .Select(b => new BuildingResponse(b.Id, b.Code, b.Name))];
    }
}
