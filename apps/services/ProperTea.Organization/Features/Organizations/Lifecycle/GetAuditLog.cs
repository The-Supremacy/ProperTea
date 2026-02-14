using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetAuditLogQuery(string OrganizationId);

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

    public record OrganizationLinked(string OrganizationId);

    public record OrganizationActivated(string OldStatus, string NewStatus);

    public record NameChanged(string OldName, string NewName);

    public record SlugChanged(string OldSlug, string NewSlug);

    public record OrganizationDeactivated(string Reason);
}

public record AuditLogResponse(
    string OrganizationId,
    IReadOnlyList<AuditLogEntry> Entries
);

public class GetAuditLogHandler(IQuerySession session) : IWolverineHandler
{
    public async Task<AuditLogResponse> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        var org = await session.Query<OrganizationAggregate>()
            .FirstOrDefaultAsync(x => x.OrganizationId == query.OrganizationId, ct)
            ?? throw new NotFoundException(
                OrganizationErrorCodes.NOT_FOUND,
                nameof(OrganizationAggregate),
                query.OrganizationId);
        var events = await session.Events.FetchStreamAsync(org.Id, token: ct);

        var entries = new List<AuditLogEntry>();
        OrganizationAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new AuditEventData.OrganizationCreated(e.CreatedAt),
                OrganizationLinked e => new AuditEventData.OrganizationLinked(e.OrganizationId),
                Activated e => new AuditEventData.OrganizationActivated(
                    OldStatus: previousState?.CurrentStatus.ToString() ?? "Pending",
                    NewStatus: "Active"),
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
                case OrganizationLinked e: previousState.Apply(e); break;
                case Activated e: previousState.Apply(e); break;
                default:
                    break;
            }
        }

        return new AuditLogResponse(query.OrganizationId, entries);
    }
}
