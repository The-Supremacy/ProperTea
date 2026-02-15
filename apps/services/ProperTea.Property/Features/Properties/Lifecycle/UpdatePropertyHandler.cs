using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record UpdateProperty(
    Guid PropertyId,
    string? Code,
    string? Name,
    string? Address);

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

        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(command.Code) && property.Code != command.Code)
        {
            // Validate code uniqueness within company (exclude self)
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

            events.Add(property.UpdateCode(command.Code));
        }

        if (!string.IsNullOrWhiteSpace(command.Name) && property.Name != command.Name)
        {
            events.Add(property.UpdateName(command.Name));
        }

        if (!string.IsNullOrWhiteSpace(command.Address) && property.Address != command.Address)
        {
            events.Add(property.UpdateAddress(command.Address));
        }

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.PropertyId, [.. events]);
            await session.SaveChangesAsync();

            await bus.PublishAsync(new PropertyIntegrationEvents.PropertyUpdated
            {
                PropertyId = command.PropertyId,
                OrganizationId = session.TenantId,
                Code = command.Code ?? property.Code,
                Name = command.Name ?? property.Name,
                Address = command.Address ?? property.Address,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
