using ProperTea.Infrastructure.Common.Address;

namespace ProperTea.Property.Features.Units;

public static class UnitEvents
{
    public record Created(
        Guid UnitId,
        Guid PropertyId,
        Guid? BuildingId,
        Guid? EntranceId,
        string Code,
        string UnitReference,
        UnitCategory Category,
        Address Address,
        int? Floor,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid UnitId,
        string OldCode,
        string NewCode);

    public record UnitReferenceRegenerated(
        Guid UnitId,
        string OldReference,
        string NewReference);

    public record CategoryChanged(
        Guid UnitId,
        UnitCategory OldCategory,
        UnitCategory NewCategory);

    public record LocationChanged(
        Guid UnitId,
        Guid OldPropertyId,
        Guid NewPropertyId,
        Guid? OldBuildingId,
        Guid? NewBuildingId,
        Guid? OldEntranceId,
        Guid? NewEntranceId);

    public record AddressUpdated(
        Guid UnitId,
        Address Address);

    public record FloorUpdated(
        Guid UnitId,
        int? OldFloor,
        int? NewFloor);

    public record Deleted(
        Guid UnitId,
        DateTimeOffset DeletedAt);
}
