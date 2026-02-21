using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Properties;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record CreateBuilding(Guid PropertyId, string Code, string Name, AddressRequest? Address);

public class CreateBuildingHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateBuilding command,
        IDocumentSession session,
        IMessageBus bus)
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

        // Use explicitly provided address, or inherit from parent property
        var address = command.Address?.ToAddress() ?? property.Address;

        var buildingId = Guid.NewGuid();
        var created = BuildingAggregate.Create(
            buildingId,
            command.PropertyId,
            command.Code,
            command.Name,
            address,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<BuildingAggregate>(buildingId, created);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new BuildingIntegrationEvents.BuildingCreated
        {
            BuildingId = buildingId,
            PropertyId = command.PropertyId,
            OrganizationId = organizationId,
            Code = command.Code,
            Name = command.Name,
            Address = new AddressData(
                address.Country.ToString(),
                address.City,
                address.ZipCode,
                address.StreetAddress),
            CreatedAt = created.CreatedAt
        });

        return buildingId;
    }
}
