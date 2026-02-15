using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record GetProperty(Guid PropertyId);

public record PropertyResponse(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    string Status,
    DateTimeOffset CreatedAt);

public class GetPropertyHandler : IWolverineHandler
{
    public async Task<PropertyResponse?> Handle(
        GetProperty query,
        IDocumentSession session)
    {
        var property = await session.Events.AggregateStreamAsync<PropertyAggregate>(query.PropertyId);

        if (property == null)
            return null;

        return new PropertyResponse(
            property.Id,
            property.CompanyId,
            property.Code,
            property.Name,
            property.Address,
            property.CurrentStatus.ToString(),
            property.CreatedAt);
    }
}
