namespace ProperTea.Landlord.Bff.Units;

public record ListUnitsQuery
{
    public string? Code { get; init; }
    public string? UnitReference { get; init; }
    public string? Category { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? BuildingId { get; init; }
    public int? Floor { get; init; }
}
