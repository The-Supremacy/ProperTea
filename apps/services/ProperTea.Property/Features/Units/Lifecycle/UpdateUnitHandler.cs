using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Buildings;
using ProperTea.Property.Features.Companies;
using ProperTea.Property.Features.Properties;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record UpdateUnit(
    Guid UnitId,
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    UnitCategory Category,
    AddressRequest Address,
    int? Floor);

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

        // Category-based building enforcement
        if (command.Category == UnitCategory.Apartment && !command.BuildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_REQUIRED,
                "Apartment units must be assigned to a building");

        if (command.Category == UnitCategory.House && command.BuildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_NOT_ALLOWED,
                "House units cannot be assigned to a building");

        // Validate target property exists and is active
        var targetProperty = await session.LoadAsync<PropertyAggregate>(command.PropertyId)
            ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND, "Property", command.PropertyId);

        if (targetProperty.CurrentStatus == PropertyAggregate.Status.Deleted)
            throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND, "Property", command.PropertyId);

        // Validate code uniqueness within the target property (exclude self)
        if (unit.Code != command.Code || unit.PropertyId != command.PropertyId)
        {
            var codeExists = await session.Query<UnitAggregate>()
                .Where(u => u.PropertyId == command.PropertyId
                    && u.Code == command.Code
                    && u.CurrentStatus == UnitAggregate.Status.Active
                    && u.Id != command.UnitId)
                .AnyAsync();

            if (codeExists)
                throw new ConflictException(
                    UnitErrorCodes.UNIT_CODE_ALREADY_EXISTS,
                    $"A unit with code '{command.Code}' already exists in this property");
        }

        // Load building, validate ownership against the target property, validate entrance
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

        // Regenerate UnitReference if code, property, or building changed
        var needsRefRegen = unit.Code != command.Code
            || unit.PropertyId != command.PropertyId
            || unit.BuildingId != command.BuildingId;
        string unitReference;
        if (needsRefRegen)
        {
            var companyRef = await session.LoadAsync<CompanyReference>(targetProperty.CompanyId)
                ?? throw new NotFoundException(
                    UnitErrorCodes.COMPANY_REF_NOT_FOUND, "CompanyReference", targetProperty.CompanyId);

            unitReference = building != null
                ? $"{companyRef.Code}-{targetProperty.Code}-{building.Code}-{command.Code}"
                : $"{companyRef.Code}-{targetProperty.Code}-{command.Code}";
        }
        else
        {
            unitReference = unit.UnitReference;
        }

        var address = command.Address.ToAddress();

        // Build the list of granular domain events
        var events = new List<object>();

        if (unit.Code != command.Code)
            events.Add(unit.UpdateCode(command.Code));

        if (command.Category != unit.Category)
            events.Add(unit.ChangeCategory(command.Category, command.BuildingId));

        if (unit.PropertyId != command.PropertyId || unit.BuildingId != command.BuildingId || unit.EntranceId != command.EntranceId)
            events.Add(unit.ChangeLocation(command.PropertyId, command.BuildingId, command.EntranceId, command.Category));

        if (!unit.Address.Equals(address))
            events.Add(unit.UpdateAddress(address));

        if (unit.Floor != command.Floor)
            events.Add(unit.UpdateFloor(command.Floor));

        // Reference needs regeneration if code or building changed
        if (needsRefRegen)
            events.Add(unit.RegenerateReference(unitReference));

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.UnitId, [.. events]);
            await session.SaveChangesAsync();

            var organizationId = session.TenantId;
            await bus.PublishAsync(new UnitIntegrationEvents.UnitUpdated
            {
                UnitId = command.UnitId,
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
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
