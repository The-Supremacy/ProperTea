using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;
using static ProperTea.Company.Features.Companies.CompanyEvents;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record GetCompanyAuditLogQuery(Guid CompanyId);

public record CompanyAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data
);

public static class CompanyAuditEventData
{
    public record CompanyCreated(string Code, string Name, DateTimeOffset CreatedAt);

    public record CodeChanged(string OldCode, string NewCode);

    public record NameChanged(string OldName, string NewName);

    public record CompanyDeleted(DateTimeOffset DeletedAt);
}

public record CompanyAuditLogResponse(
    Guid CompanyId,
    IReadOnlyList<CompanyAuditLogEntry> Entries
);

public class GetCompanyAuditLogHandler(IQuerySession session) : IWolverineHandler
{
    public async Task<CompanyAuditLogResponse> Handle(GetCompanyAuditLogQuery query, CancellationToken ct)
    {
        var events = await session.Events.FetchStreamAsync(query.CompanyId, token: ct);

        if (events.Count == 0)
            throw new NotFoundException(
                CompanyErrorCodes.COMPANY_NOT_FOUND,
                nameof(CompanyAggregate),
                query.CompanyId);

        var entries = new List<CompanyAuditLogEntry>();
        CompanyAggregate? previousState = null;

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new CompanyAuditEventData.CompanyCreated(e.Code, e.Name, e.CreatedAt),
                CodeUpdated e => new CompanyAuditEventData.CodeChanged(
                    OldCode: previousState?.Code ?? "",
                    NewCode: e.Code),
                NameUpdated e => new CompanyAuditEventData.NameChanged(
                    OldName: previousState?.Name ?? "",
                    NewName: e.Name),
                Deleted e => new CompanyAuditEventData.CompanyDeleted(e.DeletedAt),
                _ => new { EventData = evt.Data }
            };

            entries.Add(new CompanyAuditLogEntry(
                EventType: evt.EventTypeName,
                Timestamp: evt.Timestamp,
                Username: evt.UserName,
                Version: (int)evt.Version,
                Data: data
            ));

            // Rebuild state after each event for next iteration
            previousState ??= new CompanyAggregate();
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

        return new CompanyAuditLogResponse(query.CompanyId, entries);
    }
}
