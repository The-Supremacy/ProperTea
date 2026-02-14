using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record UpdateProperty(
    Guid PropertyId,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public class UpdatePropertyHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateProperty command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        // Validate code uniqueness within company (exclude self)
        if (property.Code != command.Code)
        {
            var codeExists = await session.Query<PropertyAggregate>()
                .Where(p => p.CompanyId == property.CompanyId
                    && p.Code == command.Code
                    && p.CurrentStatus == PropertyAggregate.Status.Active
                    && p.Id != command.PropertyId)
                .AnyAsync();

            if (codeExists)
                throw new ConflictException(
                    PropertyErrorCodes.PROPERTY_CODE_ALREADY_EXISTS,
                    $"A property with code '{command.Code}' already exists in this company");
        }

        var updated = property.Update(
            command.Code,
            command.Name,
            command.Address,
            command.SquareFootage);

        _ = session.Events.Append(command.PropertyId, updated);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new PropertyIntegrationEvents.PropertyUpdated
        {
            PropertyId = command.PropertyId,
            OrganizationId = organizationId,
            Code = command.Code,
            Name = command.Name,
            Address = command.Address,
            SquareFootage = command.SquareFootage,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
