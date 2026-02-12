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
    decimal? SquareFootage,
    List<BuildingResponse> Buildings,
    string Status,
    DateTimeOffset CreatedAt);

public record BuildingResponse(
    Guid Id,
    string Code,
    string Name);

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
            property.SquareFootage,
            [.. property.Buildings
                .Where(b => !b.IsRemoved)
                .Select(b => new BuildingResponse(b.Id, b.Code, b.Name))],
            property.CurrentStatus.ToString(),
            property.CreatedAt);
    }
}
