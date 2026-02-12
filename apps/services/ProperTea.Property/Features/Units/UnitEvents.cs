namespace ProperTea.Property.Features.Units;

public static class UnitEvents
{
    public record Created(
        Guid UnitId,
        Guid PropertyId,
        Guid? BuildingId,
        string Code,
        string UnitNumber,
        UnitCategory Category,
        int? Floor,
        decimal? SquareFootage,
        int? RoomCount,
        DateTimeOffset CreatedAt);

    public record Updated(
        Guid UnitId,
        Guid? BuildingId,
        string Code,
        string UnitNumber,
        UnitCategory Category,
        int? Floor,
        decimal? SquareFootage,
        int? RoomCount);

    public record Deleted(
        Guid UnitId,
        DateTimeOffset DeletedAt);
}
