using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Property.Features.Properties.PropertyEvents;

namespace ProperTea.Property.Features.Properties;

public class Building
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsRemoved { get; set; }
}

public class PropertyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal? SquareFootage { get; set; }
    public List<Building> Buildings { get; set; } = [];
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(
        Guid id,
        Guid companyId,
        string code,
        string name,
        string address,
        decimal? squareFootage,
        DateTimeOffset createdAt)
    {
        if (companyId == Guid.Empty)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_COMPANY_REQUIRED,
                "Property must be owned by a company");

        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_NAME_REQUIRED,
                "Property name is required");

        if (string.IsNullOrWhiteSpace(address))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ADDRESS_REQUIRED,
                "Property address is required");

        return new Created(id, companyId, code, name, address, squareFootage, createdAt);
    }

    public Updated Update(
        string code,
        string name,
        string address,
        decimal? squareFootage)
    {
        EnsureNotDeleted();
        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_NAME_REQUIRED,
                "Property name is required");

        if (string.IsNullOrWhiteSpace(address))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ADDRESS_REQUIRED,
                "Property address is required");

        return new Updated(Id, code, name, address, squareFootage);
    }

    public Deleted Delete(DateTimeOffset deletedAt)
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ALREADY_DELETED,
                "Property is already deleted");

        return new Deleted(Id, deletedAt);
    }

    public BuildingAdded AddBuilding(Guid buildingId, string code, string name)
    {
        EnsureNotDeleted();
        ValidateBuildingCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.BUILDING_NAME_REQUIRED,
                "Building name is required");

        if (Buildings.Any(b => !b.IsRemoved && b.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException(
                PropertyErrorCodes.BUILDING_CODE_ALREADY_EXISTS,
                $"A building with code '{code}' already exists in this property");

        return new BuildingAdded(Id, buildingId, code, name);
    }

    public BuildingUpdated UpdateBuilding(Guid buildingId, string code, string name)
    {
        EnsureNotDeleted();
        ValidateBuildingCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.BUILDING_NAME_REQUIRED,
                "Building name is required");

        var building = Buildings.FirstOrDefault(b => b.Id == buildingId && !b.IsRemoved)
            ?? throw new NotFoundException(
                PropertyErrorCodes.BUILDING_NOT_FOUND,
                "Building",
                buildingId);

        if (Buildings.Any(b => !b.IsRemoved && b.Id != buildingId && b.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException(
                PropertyErrorCodes.BUILDING_CODE_ALREADY_EXISTS,
                $"A building with code '{code}' already exists in this property");

        return new BuildingUpdated(Id, buildingId, code, name);
    }

    public BuildingRemoved RemoveBuilding(Guid buildingId)
    {
        EnsureNotDeleted();

        _ = Buildings.FirstOrDefault(b => b.Id == buildingId && !b.IsRemoved)
            ?? throw new NotFoundException(
                PropertyErrorCodes.BUILDING_NOT_FOUND,
                "Building",
                buildingId);

        return new BuildingRemoved(Id, buildingId);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.PropertyId;
        CompanyId = e.CompanyId;
        Code = e.Code;
        Name = e.Name;
        Address = e.Address;
        SquareFootage = e.SquareFootage;
        CreatedAt = e.CreatedAt;
        CurrentStatus = Status.Active;
    }

    public void Apply(Updated e)
    {
        Code = e.Code;
        Name = e.Name;
        Address = e.Address;
        SquareFootage = e.SquareFootage;
    }

    public void Apply(Deleted e)
    {
        CurrentStatus = Status.Deleted;
    }

    public void Apply(BuildingAdded e)
    {
        Buildings.Add(new Building
        {
            Id = e.BuildingId,
            Code = e.Code,
            Name = e.Name
        });
    }

    public void Apply(BuildingUpdated e)
    {
        var building = Buildings.First(b => b.Id == e.BuildingId);
        building.Code = e.Code;
        building.Name = e.Name;
    }

    public void Apply(BuildingRemoved e)
    {
        var building = Buildings.First(b => b.Id == e.BuildingId);
        building.IsRemoved = true;
    }

    #endregion

    private void EnsureNotDeleted()
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ALREADY_DELETED,
                "Cannot modify a deleted property");
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_CODE_REQUIRED,
                "Property code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_CODE_TOO_LONG,
                "Property code cannot exceed 50 characters");
    }

    private static void ValidateBuildingCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                PropertyErrorCodes.BUILDING_CODE_REQUIRED,
                "Building code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                PropertyErrorCodes.BUILDING_CODE_TOO_LONG,
                "Building code cannot exceed 50 characters");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
