using Marten.Metadata;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Infrastructure.Common.Validation;
using static ProperTea.Property.Features.Units.UnitEvents;

namespace ProperTea.Property.Features.Units;

public enum UnitCategory
{
    Apartment = 1,
    Commercial = 2,
    Parking = 3,
    House = 4,
    Other = 99
}

public class UnitAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? EntranceId { get; set; }
    public string Code { get; set; } = null!;
    public string UnitReference { get; set; } = null!;
    public UnitCategory Category { get; set; }
    public Address Address { get; set; } = null!;
    public int? Floor { get; set; }
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(
        Guid id,
        Guid propertyId,
        Guid? buildingId,
        Guid? entranceId,
        string code,
        string unitReference,
        UnitCategory category,
        Address address,
        int? floor,
        DateTimeOffset createdAt)
    {
        if (propertyId == Guid.Empty)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_PROPERTY_REQUIRED,
                "Unit must belong to a property");

        ValidateCode(code);
        ValidateBuildingRules(category, buildingId);
        ValidateEntranceRules(entranceId, buildingId);
        ValidateAddress(address);

        return new Created(id, propertyId, buildingId, entranceId, code, unitReference,
            category, address, floor, createdAt);
    }

    public CodeUpdated UpdateCode(string newCode)
    {
        EnsureNotDeleted();
        ValidateCode(newCode);
        return new CodeUpdated(Id, Code, newCode);
    }

    public UnitReferenceRegenerated RegenerateReference(string newReference)
    {
        EnsureNotDeleted();
        return new(Id, UnitReference, newReference);
    }

    public CategoryChanged ChangeCategory(UnitCategory newCategory, Guid? buildingId)
    {
        EnsureNotDeleted();
        ValidateBuildingRules(newCategory, buildingId);
        return new CategoryChanged(Id, Category, newCategory);
    }

    public LocationChanged ChangeLocation(
        Guid newPropertyId, Guid? newBuildingId, Guid? newEntranceId, UnitCategory currentCategory)
    {
        EnsureNotDeleted();
        ValidateBuildingRules(currentCategory, newBuildingId);
        ValidateEntranceRules(newEntranceId, newBuildingId);
        return new LocationChanged(Id, PropertyId, newPropertyId, BuildingId, newBuildingId, EntranceId, newEntranceId);
    }

    public AddressUpdated UpdateAddress(Address address)
    {
        EnsureNotDeleted();
        ValidateAddress(address);
        return new AddressUpdated(Id, address);
    }

    public FloorUpdated UpdateFloor(int? newFloor)
    {
        EnsureNotDeleted();
        return new FloorUpdated(Id, Floor, newFloor);
    }

    public Deleted Delete(DateTimeOffset deletedAt)
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ALREADY_DELETED,
                "Unit is already deleted");

        return new Deleted(Id, deletedAt);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.UnitId;
        PropertyId = e.PropertyId;
        BuildingId = e.BuildingId;
        EntranceId = e.EntranceId;
        Code = e.Code;
        UnitReference = e.UnitReference;
        Category = e.Category;
        Address = e.Address;
        Floor = e.Floor;
        CreatedAt = e.CreatedAt;
        CurrentStatus = Status.Active;
    }

    public void Apply(CodeUpdated e)
    {
        Code = e.NewCode;
    }

    public void Apply(UnitReferenceRegenerated e)
    {
        UnitReference = e.NewReference;
    }

    public void Apply(CategoryChanged e)
    {
        Category = e.NewCategory;
    }

    public void Apply(LocationChanged e)
    {
        PropertyId = e.NewPropertyId;
        BuildingId = e.NewBuildingId;
        EntranceId = e.NewEntranceId;
    }

    public void Apply(AddressUpdated e)
    {
        Address = e.Address;
    }

    public void Apply(FloorUpdated e)
    {
        Floor = e.NewFloor;
    }

    public void Apply(Deleted e)
    {
        CurrentStatus = Status.Deleted;
    }

    #endregion

    private void EnsureNotDeleted()
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ALREADY_DELETED,
                "Cannot update a deleted unit");
    }

    private static void ValidateCode(string code)
    {
        CodeValidator.Validate(
            code,
            maxLength: 10,
            errorRequired: UnitErrorCodes.UNIT_CODE_REQUIRED,
            errorTooLong: UnitErrorCodes.UNIT_CODE_TOO_LONG,
            errorInvalidFormat: UnitErrorCodes.UNIT_CODE_INVALID_FORMAT);
    }

    private static void ValidateBuildingRules(UnitCategory category, Guid? buildingId)
    {
        if (category == UnitCategory.Apartment && !buildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_REQUIRED,
                "Apartment units must be assigned to a building");

        if (category == UnitCategory.House && buildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_BUILDING_NOT_ALLOWED,
                "House units cannot be assigned to a building");
    }

    private static void ValidateEntranceRules(Guid? entranceId, Guid? buildingId)
    {
        if (entranceId.HasValue && !buildingId.HasValue)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ENTRANCE_REQUIRES_BUILDING,
                "An entrance can only be set when the unit belongs to a building");
    }

    private static void ValidateAddress(Address address)
    {
        if (string.IsNullOrWhiteSpace(address?.City))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ADDRESS_REQUIRED,
                "Unit address city is required");

        if (string.IsNullOrWhiteSpace(address?.ZipCode))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ADDRESS_REQUIRED,
                "Unit address zip code is required");

        if (string.IsNullOrWhiteSpace(address?.StreetAddress))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_ADDRESS_REQUIRED,
                "Unit street address is required");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}

