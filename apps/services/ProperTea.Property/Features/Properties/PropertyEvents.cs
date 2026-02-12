namespace ProperTea.Property.Features.Properties;

public static class PropertyEvents
{
    public record Created(
        Guid PropertyId,
        Guid CompanyId,
        string Code,
        string Name,
        string Address,
        decimal? SquareFootage,
        DateTimeOffset CreatedAt);

    public record Updated(
        Guid PropertyId,
        string Code,
        string Name,
        string Address,
        decimal? SquareFootage);

    public record Deleted(
        Guid PropertyId,
        DateTimeOffset DeletedAt);

    // Building child entity events
    public record BuildingAdded(
        Guid PropertyId,
        Guid BuildingId,
        string Code,
        string Name);

    public record BuildingUpdated(
        Guid PropertyId,
        Guid BuildingId,
        string Code,
        string Name);

    public record BuildingRemoved(
        Guid PropertyId,
        Guid BuildingId);
}
