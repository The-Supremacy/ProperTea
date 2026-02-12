using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record GetUnit(Guid UnitId);

public record UnitResponse(
    Guid Id,
    Guid PropertyId,
    string UnitNumber,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount,
    string Status,
    DateTimeOffset CreatedAt);

public class GetUnitHandler : IWolverineHandler
{
    public async Task<UnitResponse?> Handle(
        GetUnit query,
        IDocumentSession session)
    {
        var unit = await session.Events.AggregateStreamAsync<UnitAggregate>(query.UnitId);

        if (unit == null)
            return null;

        return new UnitResponse(
            unit.Id,
            unit.PropertyId,
            unit.UnitNumber,
            unit.Floor,
            unit.SquareFootage,
            unit.RoomCount,
            unit.CurrentStatus.ToString(),
            unit.CreatedAt);
    }
}
