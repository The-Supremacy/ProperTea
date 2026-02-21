using JasperFx.Events;
using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Buildings;
using ProperTea.Property.Features.Units;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record DeleteProperty(Guid PropertyId);

public class DeletePropertyHandler : IWolverineHandler
{
    public async Task Handle(
        DeleteProperty command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        // Block deletion if property has active buildings
        var buildingCount = await session.Query<BuildingAggregate>()
            .Where(b => b.PropertyId == command.PropertyId && b.CurrentStatus == BuildingAggregate.Status.Active)
            .CountAsync();

        if (buildingCount > 0)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_HAS_ACTIVE_BUILDINGS,
                $"Cannot delete property because it has {buildingCount} active building(s). Remove all buildings first.",
                new Dictionary<string, object> { ["buildingCount"] = buildingCount });

        // Block deletion if property has active units (e.g. house-type units without a building)
        var unitCount = await session.Query<UnitAggregate>()
            .Where(u => u.PropertyId == command.PropertyId && u.CurrentStatus == UnitAggregate.Status.Active)
            .CountAsync();

        if (unitCount > 0)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_HAS_ACTIVE_UNITS,
                $"Cannot delete property because it has {unitCount} active unit(s). Remove all units first.",
                new Dictionary<string, object> { ["unitCount"] = unitCount });

        var deleted = property.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.PropertyId, deleted, new Archived("Property deleted"));
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new PropertyIntegrationEvents.PropertyDeleted
        {
            PropertyId = command.PropertyId,
            OrganizationId = organizationId,
            DeletedAt = deleted.DeletedAt
        });
    }
}
