namespace ProperTea.Contracts.Events;

public interface IPropertyCreated
{
    public Guid PropertyId { get; }
    public Guid OrganizationId { get; }
    public Guid CompanyId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Address { get; }
    public decimal? SquareFootage { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface IPropertyUpdated
{
    public Guid PropertyId { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Address { get; }
    public decimal? SquareFootage { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface IPropertyDeleted
{
    public Guid PropertyId { get; }
    public Guid OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}

public interface IUnitCreated
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public Guid? BuildingId { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string UnitNumber { get; }
    public string Category { get; }
    public int? Floor { get; }
    public decimal? SquareFootage { get; }
    public int? RoomCount { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface IUnitUpdated
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public Guid? BuildingId { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string UnitNumber { get; }
    public string Category { get; }
    public int? Floor { get; }
    public decimal? SquareFootage { get; }
    public int? RoomCount { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface IUnitDeleted
{
    public Guid UnitId { get; }
    public Guid PropertyId { get; }
    public Guid OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}
