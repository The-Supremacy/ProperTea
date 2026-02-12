namespace ProperTea.Landlord.Bff.Property;

public record ListPropertiesQuery
{
    public string? Name { get; init; }
    public string? Code { get; init; }
    public Guid? CompanyId { get; init; }
}
