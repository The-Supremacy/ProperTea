using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record GetPropertyAuditLog(Guid PropertyId);

public record PropertyAuditLogEntry(
    long Sequence,
    string EventType,
    object? Data,
    DateTimeOffset Timestamp,
    string? UserId);

public class GetPropertyAuditLogHandler : IWolverineHandler
{
    public async Task<List<PropertyAuditLogEntry>> Handle(
        GetPropertyAuditLog query,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(
            query.PropertyId) ?? throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_NOT_FOUND,
                "Property",
                query.PropertyId);
        var events = await session.Events.FetchStreamAsync(query.PropertyId);

        return [.. events.Select(e => new PropertyAuditLogEntry(
            e.Sequence,
            e.EventTypeName,
            e.Data,
            e.Timestamp,
            e.DotNetTypeName
        ))];
    }
}
