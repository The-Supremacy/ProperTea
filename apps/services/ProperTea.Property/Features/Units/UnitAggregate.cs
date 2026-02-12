using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Property.Features.Units.UnitEvents;

namespace ProperTea.Property.Features.Units;

public enum UnitCategory
{
    Apartment = 1,
    Commercial = 2,
    Parking = 3,
    Other = 99
}

public class UnitAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? BuildingId { get; set; }
    public string Code { get; set; } = null!;
    public string UnitNumber { get; set; } = null!;
    public UnitCategory Category { get; set; }
    public int? Floor { get; set; }
    public decimal? SquareFootage { get; set; }
    public int? RoomCount { get; set; }
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(
        Guid id,
        Guid propertyId,
        Guid? buildingId,
        string code,
        string unitNumber,
        UnitCategory category,
        int? floor,
        decimal? squareFootage,
        int? roomCount,
        DateTimeOffset createdAt)
    {
        if (propertyId == Guid.Empty)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_PROPERTY_REQUIRED,
                "Unit must belong to a property");

        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_NUMBER_REQUIRED,
                "Unit number is required");

        return new Created(id, propertyId, buildingId, code, unitNumber, category, floor, squareFootage, roomCount, createdAt);
    }

    public Updated Update(
        Guid? buildingId,
        string code,
        string unitNumber,
        UnitCategory category,
        int? floor,
        decimal? squareFootage,
        int? roomCount)
    {
        EnsureNotDeleted();
        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_NUMBER_REQUIRED,
                "Unit number is required");

        return new Updated(Id, buildingId, code, unitNumber, category, floor, squareFootage, roomCount);
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
        Code = e.Code;
        UnitNumber = e.UnitNumber;
        Category = e.Category;
        Floor = e.Floor;
        SquareFootage = e.SquareFootage;
        RoomCount = e.RoomCount;
        CreatedAt = e.CreatedAt;
        CurrentStatus = Status.Active;
    }

    public void Apply(Updated e)
    {
        BuildingId = e.BuildingId;
        Code = e.Code;
        UnitNumber = e.UnitNumber;
        Category = e.Category;
        Floor = e.Floor;
        SquareFootage = e.SquareFootage;
        RoomCount = e.RoomCount;
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
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_CODE_REQUIRED,
                "Unit code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                UnitErrorCodes.UNIT_CODE_TOO_LONG,
                "Unit code cannot exceed 50 characters");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
