using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetAuditLogQuery(Guid OrganizationId);

public record AuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data
);

public static class AuditEventData
{
    public record OrganizationCreated(DateTimeOffset CreatedAt);

    public record ExternalOrganizationCreated(string ExternalOrganizationId);

    public record OrganizationActivated;

    public record NameChanged(string OldName, string NewName);

    public record SlugChanged(string OldSlug, string NewSlug);

    public record OrganizationDeactivated(string Reason);
}

public record AuditLogResponse(
    Guid OrganizationId,
    IReadOnlyList<AuditLogEntry> Entries
);

public class GetAuditLogHandler(IQuerySession session)
{
    public async Task<AuditLogResponse> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        var events = await session.Events.FetchStreamAsync(query.OrganizationId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(
                OrganizationErrorCodes.NOT_FOUND,
                nameof(OrganizationAggregate),
                query.OrganizationId);

        var entries = new List<AuditLogEntry>();
        OrganizationAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new AuditEventData.OrganizationCreated(e.CreatedAt),
                ExternalOrganizationCreated e => new AuditEventData.ExternalOrganizationCreated(e.ExternalOrganizationId),
                Activated => new AuditEventData.OrganizationActivated(),
                _ => new { EventData = evt.Data }
            };

            entries.Add(new AuditLogEntry(
                EventType: evt.EventTypeName,
                Timestamp: evt.Timestamp,
                Username: evt.UserName,
                Version: (int)evt.Version,
                Data: data
            ));

            // Rebuild state after each event for next iteration
            previousState ??= new OrganizationAggregate();
            switch (evt.Data)
            {
                case Created e: previousState.Apply(e); break;
                case ExternalOrganizationCreated e: previousState.Apply(e); break;
                case Activated e: previousState.Apply(e); break;
                default:
                    break;
            }
        }

        return new AuditLogResponse(query.OrganizationId, entries);
    }
}
