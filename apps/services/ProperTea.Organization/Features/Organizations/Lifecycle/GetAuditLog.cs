using Marten;
using ProperTea.ServiceDefaults.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetAuditLogQuery(Guid OrganizationId);

public record AuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? UserId,
    int Version,
    object Data // Event-specific data for interpolation
);

/// <summary>
/// Event-specific data objects for frontend interpolation
/// </summary>
public static class AuditEventData
{
    public record OrganizationCreated(string Name, string Slug);

    public record ZitadelLinked(string ZitadelOrganizationId);

    public record OrganizationActivated;

    public record NameChanged(string OldName, string NewName);

    public record SlugChanged(string OldSlug, string NewSlug);

    public record OrganizationDeactivated(string Reason);
}

public record AuditLogResponse(
    Guid OrganizationId,
    IReadOnlyList<AuditLogEntry> Entries
);

public class GetAuditLogHandler(IDocumentSession session)
{
    public async Task<AuditLogResponse> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        // Fetch all events for the stream
        var events = await session.Events.FetchStreamAsync(query.OrganizationId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(nameof(OrganizationAggregate), query.OrganizationId);

        // Build state progressively to provide "old value" context
        var entries = new List<AuditLogEntry>();
        OrganizationAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new AuditEventData.OrganizationCreated(e.Name, e.Slug),
                ZitadelOrganizationCreated e => new AuditEventData.ZitadelLinked(e.ZitadelOrganizationId),
                Activated => new AuditEventData.OrganizationActivated(),
                NameChanged e => new AuditEventData.NameChanged(previousState?.Name ?? "", e.NewName),
                SlugChanged e => new AuditEventData.SlugChanged(previousState?.Slug ?? "", e.NewSlug),
                Deactivated e => new AuditEventData.OrganizationDeactivated(e.Reason),
                _ => new { EventData = evt.Data }
            };

            entries.Add(new AuditLogEntry(
                EventType: evt.Data.GetType().Name,
                Timestamp: evt.Timestamp,
                UserId: null, // TODO: Extract from metadata if needed
                Version: (int)evt.Version,
                Data: data
            ));

            // Rebuild state after each event for next iteration
            previousState ??= new OrganizationAggregate();
            switch (evt.Data)
            {
                case Created e: previousState.Apply(e); break;
                case ZitadelOrganizationCreated e: previousState.Apply(e); break;
                case Activated e: previousState.Apply(e); break;
                case NameChanged e: previousState.Apply(e); break;
                case SlugChanged e: previousState.Apply(e); break;
                case Deactivated e: previousState.Apply(e); break;
                default:
                    break;
            }
        }

        return new AuditLogResponse(query.OrganizationId, entries);
    }
}
