using Marten;
using ProperTea.Infrastructure.Common.Address;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record GetUnit(Guid UnitId);

public record UnitResponse(
    Guid Id,
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    string UnitReference,
    string Category,
    Address Address,
    int? Floor,
    string Status,
    DateTimeOffset CreatedAt);

public class GetUnitHandler : IWolverineHandler
{
    public async Task<UnitResponse?> Handle(
        GetUnit query,
        IDocumentSession session)
    {
        var unit = await session.Events.AggregateStreamAsync<UnitAggregate>(query.UnitId);

        if (unit is null || unit.CurrentStatus == UnitAggregate.Status.Deleted)
            return null;

        return new UnitResponse(
            unit.Id,
            unit.PropertyId,
            unit.BuildingId,
            unit.EntranceId,
            unit.Code,
            unit.UnitReference,
            unit.Category.ToString(),
            unit.Address,
            unit.Floor,
            unit.CurrentStatus.ToString(),
            unit.CreatedAt);
    }
}
