namespace ProperTea.Property.Features.Properties;

public static class PropertyEvents
{
    public record Created(
        Guid PropertyId,
        Guid CompanyId,
        string Code,
        string Name,
        string Address,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid PropertyId,
        string Code);

    public record NameUpdated(
        Guid PropertyId,
        string Name);

    public record AddressUpdated(
        Guid PropertyId,
        string Address);

    public record Deleted(
        Guid PropertyId,
        DateTimeOffset DeletedAt);
}
