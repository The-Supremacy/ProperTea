using Marten;
using ProperTea.Property.Features.Properties;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record CreateUnit(
    Guid PropertyId,
    Guid? BuildingId,
    string Code,
    string UnitNumber,
    UnitCategory Category,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount);

public class CreateUnitHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateUnit command,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Validate that the property exists
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(
            command.PropertyId) ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        // Validate code uniqueness within property
        var codeExists = await session.Query<UnitAggregate>()
            .Where(u => u.PropertyId == command.PropertyId
                && u.Code == command.Code
                && u.CurrentStatus == UnitAggregate.Status.Active)
            .AnyAsync();

        if (codeExists)
            throw new ConflictException(
                UnitErrorCodes.UNIT_CODE_ALREADY_EXISTS,
                $"A unit with code '{command.Code}' already exists in this property");

        // Validate building belongs to property if specified
        if (command.BuildingId.HasValue)
        {
            var building = property.Buildings
                .FirstOrDefault(b => b.Id == command.BuildingId.Value && !b.IsRemoved)
                ?? throw new NotFoundException(
                    PropertyErrorCodes.BUILDING_NOT_FOUND,
                    "Building",
                    command.BuildingId.Value);
        }

        var unitId = Guid.NewGuid();
        var created = UnitAggregate.Create(
            unitId,
            command.PropertyId,
            command.BuildingId,
            command.Code,
            command.UnitNumber,
            command.Category,
            command.Floor,
            command.SquareFootage,
            command.RoomCount,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<UnitAggregate>(unitId, created);
        await session.SaveChangesAsync();

        var organizationId = Guid.Parse(session.TenantId);
        await bus.PublishAsync(new UnitIntegrationEvents.UnitCreated
        {
            UnitId = unitId,
            PropertyId = command.PropertyId,
            BuildingId = command.BuildingId,
            OrganizationId = organizationId,
            Code = command.Code,
            UnitNumber = command.UnitNumber,
            Category = command.Category.ToString(),
            Floor = command.Floor,
            SquareFootage = command.SquareFootage,
            RoomCount = command.RoomCount,
            CreatedAt = created.CreatedAt
        });

        return unitId;
    }
}
