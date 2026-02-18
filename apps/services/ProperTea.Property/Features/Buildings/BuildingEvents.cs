namespace ProperTea.Property.Features.Buildings;

public static class BuildingEvents
{
    public record Created(
        Guid BuildingId,
        Guid PropertyId,
        string Code,
        string Name,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid BuildingId,
        string Code);

    public record NameUpdated(
        Guid BuildingId,
        string Name);

    public record Deleted(
        Guid BuildingId,
        DateTimeOffset DeletedAt);
}