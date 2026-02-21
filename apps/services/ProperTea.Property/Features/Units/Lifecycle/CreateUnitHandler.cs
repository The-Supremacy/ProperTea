using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Buildings;
using ProperTea.Property.Features.Companies;
using ProperTea.Property.Features.Properties;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record CreateUnit(
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    UnitCategory Category,
    AddressRequest Address,
    int? Floor);

public class CreateUnitHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateUnit command,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Validate that the property exists and is active
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(
            command.PropertyId) ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                command.PropertyId);

        // Category-based building enforcement (also validated inside the aggregate)
        if (command.Category == UnitCategory.Apartment && !command.BuildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_REQUIRED,
                "Apartment units must be assigned to a building");

        if (command.Category == UnitCategory.House && command.BuildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_NOT_ALLOWED,
                "House units cannot be assigned to a building");

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

        // Load building, validate ownership, validate entrance if applicable
        BuildingAggregate? building = null;
        if (command.BuildingId.HasValue)
        {
            building = await session.LoadAsync<BuildingAggregate>(command.BuildingId.Value);

            if (building is null || building.CurrentStatus == BuildingAggregate.Status.Deleted)
                throw new NotFoundException(
                    BuildingErrorCodes.BUILDING_NOT_FOUND,
                    "Building",
                    command.BuildingId.Value);

            if (building.PropertyId != command.PropertyId)
                throw new BusinessViolationException(
                    UnitErrorCodes.UNIT_BUILDING_WRONG_PROPERTY,
                    "Building does not belong to this property");

            if (command.EntranceId.HasValue &&
                !building.Entrances.Any(e => e.Id == command.EntranceId.Value))
                throw new NotFoundException(
                    UnitErrorCodes.UNIT_ENTRANCE_NOT_FOUND,
                    "Entrance",
                    command.EntranceId.Value);
        }

        // Load company reference to compose the UnitReference
        var companyRef = await session.LoadAsync<CompanyReference>(property.CompanyId)
            ?? throw new NotFoundException(
                UnitErrorCodes.COMPANY_REF_NOT_FOUND,
                "CompanyReference",
                property.CompanyId);

        var unitReference = building != null
            ? $"{companyRef.Code}-{property.Code}-{building.Code}-{command.Code}"
            : $"{companyRef.Code}-{property.Code}-{command.Code}";

        // Address is explicitly required for units.
        var address = command.Address.ToAddress();

        var unitId = Guid.NewGuid();
        var created = UnitAggregate.Create(
            unitId,
            command.PropertyId,
            command.BuildingId,
            command.EntranceId,
            command.Code,
            unitReference,
            command.Category,
            address,
            command.Floor,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<UnitAggregate>(unitId, created);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new UnitIntegrationEvents.UnitCreated
        {
            UnitId = unitId,
            PropertyId = command.PropertyId,
            BuildingId = command.BuildingId,
            EntranceId = command.EntranceId,
            OrganizationId = organizationId,
            Code = command.Code,
            UnitReference = unitReference,
            Category = command.Category.ToString(),
            Address = new Contracts.Events.AddressData(
                address.Country.ToString(),
                address.City,
                address.ZipCode,
                address.StreetAddress),
            Floor = command.Floor,
            CreatedAt = created.CreatedAt
        });

        return unitId;
    }
}
