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
    public record CompanyCreated(string Name, DateTimeOffset CreatedAt);

    public record NameChanged(string NewName);

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

        foreach (var evt in events)
        {
            var data = evt.Data switch
            {
                Created e => (object)new CompanyAuditEventData.CompanyCreated(e.Name, e.CreatedAt),
                NameUpdated e => new CompanyAuditEventData.NameChanged(e.Name),
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
        }

        return new CompanyAuditLogResponse(query.CompanyId, entries);
    }
}
