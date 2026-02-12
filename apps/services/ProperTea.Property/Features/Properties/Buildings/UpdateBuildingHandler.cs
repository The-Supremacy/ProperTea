using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Buildings;

public record UpdateBuilding(Guid PropertyId, Guid BuildingId, string Code, string Name);

public class UpdateBuildingHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateBuilding command,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        var updated = property.UpdateBuilding(command.BuildingId, command.Code, command.Name);
        _ = session.Events.Append(command.PropertyId, updated);
    }
}
