using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
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

        var deleted = property.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.PropertyId, deleted);
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
