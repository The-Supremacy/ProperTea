using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Properties;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record CreateBuilding(Guid PropertyId, string Code, string Name);

public class CreateBuildingHandler : IWolverineHandler
{
    public async Task<Guid> Handle(CreateBuilding command, IDocumentSession session)
    {
        var property = await session.LoadAsync<PropertyAggregate>(command.PropertyId);

        if (property is null || property.CurrentStatus == PropertyAggregate.Status.Deleted)
            throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                nameof(PropertyAggregate),
                command.PropertyId);

        var codeExists = await session.Query<BuildingAggregate>()
            .Where(b => b.PropertyId == command.PropertyId
                && b.Code == command.Code
                && b.CurrentStatus == BuildingAggregate.Status.Active)
            .AnyAsync();

        if (codeExists)
            throw new ConflictException(
                BuildingErrorCodes.BUILDING_CODE_ALREADY_EXISTS,
                $"A building with code '{command.Code}' already exists in this property");

        var buildingId = Guid.NewGuid();
        var created = BuildingAggregate.Create(
            buildingId,
            command.PropertyId,
            command.Code,
            command.Name,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<BuildingAggregate>(buildingId, created);
        await session.SaveChangesAsync();
        return buildingId;
    }
}