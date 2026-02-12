using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Buildings;

public record AddBuilding(Guid PropertyId, string Code, string Name);

public class AddBuildingHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        AddBuilding command,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        var buildingId = Guid.NewGuid();
        var added = property.AddBuilding(buildingId, command.Code, command.Name);

        _ = session.Events.Append(command.PropertyId, added);
        await session.SaveChangesAsync();

        return buildingId;
    }
}
