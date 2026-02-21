using ProperTea.Infrastructure.Common.Address;

namespace ProperTea.Property.Features.Properties;

public static class PropertyEvents
{
    public record Created(
        Guid PropertyId,
        Guid CompanyId,
        string Code,
        string Name,
        Address Address,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid PropertyId,
        string Code);

    public record NameUpdated(
        Guid PropertyId,
        string Name);

    public record AddressUpdated(
        Guid PropertyId,
        Address Address);

    public record Deleted(
        Guid PropertyId,
        DateTimeOffset DeletedAt);
}
