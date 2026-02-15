using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;
using static ProperTea.Property.Features.Buildings.BuildingEvents;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record GetBuildingAuditLogQuery(Guid BuildingId);

public record BuildingAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public static class BuildingAuditEventData
{
    public record BuildingCreated(Guid PropertyId, string Code, string Name, DateTimeOffset CreatedAt);

    public record CodeChanged(string OldCode, string NewCode);

    public record NameChanged(string OldName, string NewName);

    public record BuildingDeleted(DateTimeOffset DeletedAt);
}

public record BuildingAuditLogResponse(
    Guid BuildingId,
    IReadOnlyList<BuildingAuditLogEntry> Entries);

public class GetBuildingAuditLogHandler(IQuerySession session) : IWolverineHandler
{
    public async Task<BuildingAuditLogResponse> Handle(GetBuildingAuditLogQuery query, CancellationToken ct)
    {
        var events = await session.Events.FetchStreamAsync(query.BuildingId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(
                BuildingErrorCodes.BUILDING_NOT_FOUND,
                nameof(BuildingAggregate),
                query.BuildingId);

        var entries = new List<BuildingAuditLogEntry>();
        BuildingAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new BuildingAuditEventData.BuildingCreated(e.PropertyId, e.Code, e.Name, e.CreatedAt),
                CodeUpdated e => new BuildingAuditEventData.CodeChanged(
                    OldCode: previousState?.Code ?? "",
                    NewCode: e.Code),
                NameUpdated e => new BuildingAuditEventData.NameChanged(
                    OldName: previousState?.Name ?? "",
                    NewName: e.Name),
                Deleted e => new BuildingAuditEventData.BuildingDeleted(e.DeletedAt),
                _ => new { EventData = evt.Data }
            };

            entries.Add(new BuildingAuditLogEntry(
                EventType: evt.EventTypeName,
                Timestamp: evt.Timestamp,
                Username: evt.UserName,
                Version: (int)evt.Version,
                Data: data
            ));

            previousState ??= new BuildingAggregate();
            switch (evt.Data)
            {
                case Created e: previousState.Apply(e); break;
                case CodeUpdated e: previousState.Apply(e); break;
                case NameUpdated e: previousState.Apply(e); break;
                case Deleted e: previousState.Apply(e); break;
                default:
                    break;
            }
        }

        return new BuildingAuditLogResponse(query.BuildingId, entries);
    }
}
