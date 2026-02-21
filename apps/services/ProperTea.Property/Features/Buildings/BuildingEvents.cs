using ProperTea.Infrastructure.Common.Address;

namespace ProperTea.Property.Features.Buildings;

public static class BuildingEvents
{
    public record Created(
        Guid BuildingId,
        Guid PropertyId,
        string Code,
        string Name,
        Address Address,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid BuildingId,
        string Code);

    public record NameUpdated(
        Guid BuildingId,
        string Name);

    public record AddressUpdated(
        Guid BuildingId,
        Address Address);

    public record EntranceAdded(
        Guid BuildingId,
        Guid EntranceId,
        string Code,
        string Name);

    public record EntranceUpdated(
        Guid BuildingId,
        Guid EntranceId,
        string Code,
        string Name);

    public record EntranceRemoved(
        Guid BuildingId,
        Guid EntranceId);

    public record Deleted(
        Guid BuildingId,
        DateTimeOffset DeletedAt);
}
