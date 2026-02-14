using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record CreateProperty(
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public class CreatePropertyHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateProperty command,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Validate code uniqueness within company

        var existingProperty = await session.Query<PropertyAggregate>()
            .Where(p => p.CompanyId == command.CompanyId
                && p.Code == command.Code
                && p.CurrentStatus == PropertyAggregate.Status.Active)
                .ToListAsync();
        var codeExists = existingProperty.Any();

        if (codeExists)
            throw new ConflictException(
                PropertyErrorCodes.PROPERTY_CODE_ALREADY_EXISTS,
                $"A property with code '{command.Code}' already exists in this company");

        var propertyId = Guid.NewGuid();
        var created = PropertyAggregate.Create(
            propertyId,
            command.CompanyId,
            command.Code,
            command.Name,
            command.Address,
            command.SquareFootage,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<PropertyAggregate>(propertyId, created);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new PropertyIntegrationEvents.PropertyCreated
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            CompanyId = command.CompanyId,
            Code = command.Code,
            Name = command.Name,
            Address = command.Address,
            SquareFootage = command.SquareFootage,
            CreatedAt = created.CreatedAt
        });

        return propertyId;
    }
}
