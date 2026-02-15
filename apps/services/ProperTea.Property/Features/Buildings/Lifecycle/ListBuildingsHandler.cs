using Marten;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Lifecycle;

public record BuildingFilters
{
    public Guid? PropertyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public record ListBuildings(
    BuildingFilters Filters,
    PaginationQuery Pagination,
    SortQuery Sort);

public record BuildingListItemResponse(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public class ListBuildingsHandler : IWolverineHandler
{
    public async Task<PagedResult<BuildingListItemResponse>> Handle(ListBuildings command, IDocumentSession session)
    {
        var normalized = command.Pagination.Normalize();

        var baseQuery = session.Query<BuildingAggregate>()
            .Where(b => b.CurrentStatus == BuildingAggregate.Status.Active);

        if (command.Filters.PropertyId.HasValue)
        {
            baseQuery = baseQuery.Where(b => b.PropertyId == command.Filters.PropertyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(command.Filters.Code))
        {
            baseQuery = baseQuery.Where(b =>
                b.Code.Contains(command.Filters.Code, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(command.Filters.Name))
        {
            baseQuery = baseQuery.Where(b =>
                b.Name.Contains(command.Filters.Name, StringComparison.OrdinalIgnoreCase));
        }

        baseQuery = ApplySorting(baseQuery, command.Sort);

        var totalCount = await baseQuery.CountAsync();

        var buildings = await baseQuery
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync();

        var items = buildings.Select(b => new BuildingListItemResponse(
            b.Id,
            b.PropertyId,
            b.Code,
            b.Name,
            b.CurrentStatus.ToString(),
            b.CreatedAt)).ToList();

        return new PagedResult<BuildingListItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = normalized.Page,
            PageSize = normalized.PageSize
        };
    }

    private static IQueryable<BuildingAggregate> ApplySorting(
        IQueryable<BuildingAggregate> query,
        SortQuery sortQuery)
    {
        if (string.IsNullOrWhiteSpace(sortQuery.Field))
            return query.OrderByDescending(b => b.CreatedAt);

        return sortQuery.Field.ToLowerInvariant() switch
        {
            "code" => sortQuery.IsDescending
                ? query.OrderByDescending(b => b.Code)
                : query.OrderBy(b => b.Code),
            "name" => sortQuery.IsDescending
                ? query.OrderByDescending(b => b.Name)
                : query.OrderBy(b => b.Name),
            "created" or "createdat" => sortQuery.IsDescending
                ? query.OrderByDescending(b => b.CreatedAt)
                : query.OrderBy(b => b.CreatedAt),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };
    }
}
