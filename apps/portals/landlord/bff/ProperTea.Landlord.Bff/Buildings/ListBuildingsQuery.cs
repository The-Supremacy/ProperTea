namespace ProperTea.Landlord.Bff.Buildings;

public record ListBuildingsQuery
{
    public string? Code { get; init; }
    public string? Name { get; init; }
    public Guid? PropertyId { get; init; }
}
