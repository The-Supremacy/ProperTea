using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Buildings;

public record RemoveBuilding(Guid PropertyId, Guid BuildingId);

public class RemoveBuildingHandler : IWolverineHandler
{
    public async Task Handle(
        RemoveBuilding command,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        var removed = property.RemoveBuilding(command.BuildingId);
        _ = session.Events.Append(command.PropertyId, removed);
    }
}
