using JasperFx.Events;
using Marten;
using ProperTea.Property.Features.Properties;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public class DeleteUnitsForPropertyHandler : IWolverineHandler
{
    public async Task Handle(
        PropertyIntegrationEvents.PropertyDeleted message,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Find all active units for the deleted property
        var units = await session.Query<UnitAggregate>()
            .Where(u => u.PropertyId == message.PropertyId && u.CurrentStatus == UnitAggregate.Status.Active)
            .ToListAsync();

        foreach (var unit in units)
        {
            var deleted = unit.Delete(message.DeletedAt);
            _ = session.Events.Append(unit.Id, deleted, new Archived("Property deleted cascade"));

            await bus.PublishAsync(new UnitIntegrationEvents.UnitDeleted
            {
                UnitId = unit.Id,
                PropertyId = unit.PropertyId,
                OrganizationId = message.OrganizationId,
                DeletedAt = message.DeletedAt
            });
        }
    }
}
