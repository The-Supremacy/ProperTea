using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;
using static ProperTea.Property.Features.Properties.PropertyEvents;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record GetPropertyAuditLogQuery(Guid PropertyId);

public record PropertyAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data
);

public static class PropertyAuditEventData
{
    public record PropertyCreated(Guid CompanyId, string Code, string Name, string Address, DateTimeOffset CreatedAt);

    public record CodeChanged(string OldCode, string NewCode);

    public record NameChanged(string OldName, string NewName);

    public record AddressChanged(string OldAddress, string NewAddress);

    public record PropertyDeleted(DateTimeOffset DeletedAt);
}

public record PropertyAuditLogResponse(
    Guid PropertyId,
    IReadOnlyList<PropertyAuditLogEntry> Entries
);

public class GetPropertyAuditLogHandler(IQuerySession session) : IWolverineHandler
{
    public async Task<PropertyAuditLogResponse> Handle(GetPropertyAuditLogQuery query, CancellationToken ct)
    {
        var events = await session.Events.FetchStreamAsync(query.PropertyId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                nameof(PropertyAggregate),
                query.PropertyId);

        var entries = new List<PropertyAuditLogEntry>();
        PropertyAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new PropertyAuditEventData.PropertyCreated(e.CompanyId, e.Code, e.Name, e.Address, e.CreatedAt),
                CodeUpdated e => new PropertyAuditEventData.CodeChanged(
                    OldCode: previousState?.Code ?? "",
                    NewCode: e.Code),
                NameUpdated e => new PropertyAuditEventData.NameChanged(
                    OldName: previousState?.Name ?? "",
                    NewName: e.Name),
                AddressUpdated e => new PropertyAuditEventData.AddressChanged(
                    OldAddress: previousState?.Address ?? "",
                    NewAddress: e.Address),
                Deleted e => new PropertyAuditEventData.PropertyDeleted(e.DeletedAt),
                _ => new { EventData = evt.Data }
            };

            entries.Add(new PropertyAuditLogEntry(
                EventType: evt.EventTypeName,
                Timestamp: evt.Timestamp,
                Username: evt.UserName,
                Version: (int)evt.Version,
                Data: data
            ));

            // Rebuild state after each event for next iteration
            previousState ??= new PropertyAggregate();
            switch (evt.Data)
            {
                case Created e: previousState.Apply(e); break;
                case CodeUpdated e: previousState.Apply(e); break;
                case NameUpdated e: previousState.Apply(e); break;
                case AddressUpdated e: previousState.Apply(e); break;
                case Deleted e: previousState.Apply(e); break;
                default:
                    break;
            }
        }

        return new PropertyAuditLogResponse(query.PropertyId, entries);
    }
}
