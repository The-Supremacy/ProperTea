namespace ProperTea.Contracts.Events;

/// <summary>Portable address representation for integration events. Country is the ISO 3166-1 alpha-2 code.</summary>
public interface IAddressData
{
    string Country { get; }
    string City { get; }
    string ZipCode { get; }
    string StreetAddress { get; }
}

public interface IPropertyCreated
{
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public Guid CompanyId { get; }
    public string Code { get; }
    public string Name { get; }
    public IAddressData Address { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface IPropertyUpdated
{
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public Guid CompanyId { get; }
    public string Code { get; }
    public string Name { get; }
    public IAddressData Address { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface IPropertyDeleted
{
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}

public interface IUnitCreated
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public Guid? BuildingId { get; }
    public Guid? EntranceId { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string UnitReference { get; }
    public string Category { get; }
    public IAddressData Address { get; }
    public int? Floor { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface IUnitUpdated
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public Guid? BuildingId { get; }
    public Guid? EntranceId { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string UnitReference { get; }
    public string Category { get; }
    public IAddressData Address { get; }
    public int? Floor { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface IUnitDeleted
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}

public interface IBuildingCreated
{
    public Guid BuildingId { get; }
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public IAddressData Address { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface IBuildingUpdated
{
    public Guid BuildingId { get; }
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public IAddressData Address { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface IBuildingDeleted
{
    public Guid BuildingId { get; }
    public Guid PropertyId { get; }
    public string OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}
