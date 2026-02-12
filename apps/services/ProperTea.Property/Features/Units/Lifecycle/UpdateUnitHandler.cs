using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record UpdateUnit(
    Guid UnitId,
    Guid? BuildingId,
    string Code,
    string UnitNumber,
    UnitCategory Category,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount);

public class UpdateUnitHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateUnit command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var unit = await session.Events.AggregateStreamAsync<UnitAggregate>(command.UnitId)
            ?? throw new NotFoundException(
                UnitErrorCodes.UNIT_NOT_FOUND,
                "Unit",
                command.UnitId);

        // Validate code uniqueness within property (exclude self)
        if (unit.Code != command.Code)
        {
            var codeExists = await session.Query<UnitAggregate>()
                .Where(u => u.PropertyId == unit.PropertyId
                    && u.Code == command.Code
                    && u.CurrentStatus == UnitAggregate.Status.Active
                    && u.Id != command.UnitId)
                .AnyAsync();

            if (codeExists)
                throw new ConflictException(
                    UnitErrorCodes.UNIT_CODE_ALREADY_EXISTS,
                    $"A unit with code '{command.Code}' already exists in this property");
        }

        var updated = unit.Update(
            command.BuildingId,
            command.Code,
            command.UnitNumber,
            command.Category,
            command.Floor,
            command.SquareFootage,
            command.RoomCount);

        _ = session.Events.Append(command.UnitId, updated);
        await session.SaveChangesAsync();

        var organizationId = Guid.Parse(session.TenantId);
        await bus.PublishAsync(new UnitIntegrationEvents.UnitUpdated
        {
            UnitId = command.UnitId,
            PropertyId = unit.PropertyId,
            BuildingId = command.BuildingId,
            OrganizationId = organizationId,
            Code = command.Code,
            UnitNumber = command.UnitNumber,
            Category = command.Category.ToString(),
            Floor = command.Floor,
            SquareFootage = command.SquareFootage,
            RoomCount = command.RoomCount,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
