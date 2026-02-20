using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;
using static ProperTea.Property.Features.Units.UnitEvents;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record GetUnitAuditLog(Guid UnitId);

public record UnitAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public static class UnitAuditEventData
{
    public record UnitCreated(Guid PropertyId, Guid? BuildingId, Guid? EntranceId, string Code, string UnitReference, string Category, int? Floor, DateTimeOffset CreatedAt);
    public record CodeUpdated(string OldCode, string NewCode);
    public record UnitReferenceRegenerated(string OldReference, string NewReference);
    public record CategoryChanged(string OldCategory, string NewCategory);
    public record LocationChanged(Guid OldPropertyId, Guid NewPropertyId, Guid? OldBuildingId, Guid? NewBuildingId, Guid? OldEntranceId, Guid? NewEntranceId);
    public record AddressUpdated(string StreetAddress, string City, string ZipCode, string Country);
    public record FloorUpdated(int? OldFloor, int? NewFloor);
    public record UnitDeleted(DateTimeOffset DeletedAt);
}

public record UnitAuditLogResponse(
    Guid UnitId,
    IReadOnlyList<UnitAuditLogEntry> Entries);

public class GetUnitAuditLogHandler(IQuerySession session) : IWolverineHandler
{
    public async Task<UnitAuditLogResponse> Handle(GetUnitAuditLog query, CancellationToken ct)
    {
        var events = await session.Events.FetchStreamAsync(query.UnitId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(
                UnitErrorCodes.UNIT_NOT_FOUND,
                nameof(UnitAggregate),
                query.UnitId);

        var entries = events.Select(evt =>
        {
            var data = evt.Data switch
            {
                Created e => (object)new UnitAuditEventData.UnitCreated(
                    e.PropertyId, e.BuildingId, e.EntranceId, e.Code, e.UnitReference,
                    e.Category.ToString(), e.Floor, e.CreatedAt),
                CodeUpdated e         => new UnitAuditEventData.CodeUpdated(e.OldCode, e.NewCode),
                UnitReferenceRegenerated e => new UnitAuditEventData.UnitReferenceRegenerated(e.OldReference, e.NewReference),
                CategoryChanged e     => new UnitAuditEventData.CategoryChanged(e.OldCategory.ToString(), e.NewCategory.ToString()),
                LocationChanged e     => new UnitAuditEventData.LocationChanged(e.OldPropertyId, e.NewPropertyId, e.OldBuildingId, e.NewBuildingId, e.OldEntranceId, e.NewEntranceId),
                AddressUpdated e      => new UnitAuditEventData.AddressUpdated(e.Address.StreetAddress, e.Address.City, e.Address.ZipCode, e.Address.Country.ToString()),
                FloorUpdated e        => new UnitAuditEventData.FloorUpdated(e.OldFloor, e.NewFloor),
                Deleted e             => new UnitAuditEventData.UnitDeleted(e.DeletedAt),
                _ => new { EventData = evt.Data }
            };

            return new UnitAuditLogEntry(
                EventType: evt.EventTypeName,
                Timestamp: evt.Timestamp,
                Username: evt.UserName,
                Version: (int)evt.Version,
                Data: data);
        }).ToList();

        return new UnitAuditLogResponse(query.UnitId, entries);
    }
}
