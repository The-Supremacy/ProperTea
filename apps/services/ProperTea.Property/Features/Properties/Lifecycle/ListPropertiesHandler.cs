using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Property.Features.Companies;
using ProperTea.Property.Features.Buildings;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record PropertyFilters
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid? CompanyId { get; set; }
}

public record ListProperties(
    PropertyFilters Filters,
    PaginationQuery Pagination,
    SortQuery Sort);

public record PropertyListItemResponse(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string Code,
    string Name,
    Address Address,
    int BuildingCount,
    string Status,
    DateTimeOffset CreatedAt);

public class ListPropertiesHandler : IWolverineHandler
{
    public async Task<PagedResult<PropertyListItemResponse>> Handle(
        ListProperties command,
        IDocumentSession session)
    {
        var normalized = command.Pagination.Normalize();

        var baseQuery = session.Query<PropertyAggregate>()
            .Where(p => p.CurrentStatus == PropertyAggregate.Status.Active);

        if (!string.IsNullOrWhiteSpace(command.Filters.Name))
        {
            baseQuery = baseQuery.Where(p =>
                p.Name.Contains(command.Filters.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(command.Filters.Code))
        {
            baseQuery = baseQuery.Where(p =>
                p.Code.Contains(command.Filters.Code, StringComparison.OrdinalIgnoreCase));
        }

        if (command.Filters.CompanyId.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.CompanyId == command.Filters.CompanyId.Value);
        }

        baseQuery = ApplySorting(baseQuery, command.Sort);

        var totalCount = await baseQuery.CountAsync();

        var properties = await baseQuery
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync();

        // Resolve company names via CompanyReference (query-time join)
        var companyIds = properties.Select(p => p.CompanyId).Distinct().ToList();
        var companies = await session.Query<CompanyReference>()
            .Where(c => c.Id.In(companyIds))
            .ToListAsync();
        var companyNameLookup = companies.ToDictionary(c => c.Id, c => c.Name);

        var propertyIds = properties.Select(p => p.Id).ToList();
        var buildings = await session.Query<BuildingAggregate>()
            .Where(b => b.PropertyId.In(propertyIds)
                && b.CurrentStatus == BuildingAggregate.Status.Active)
            .ToListAsync();
        var buildingCountLookup = buildings
            .GroupBy(b => b.PropertyId)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = properties.Select(p => new PropertyListItemResponse(
            p.Id,
            p.CompanyId,
            companyNameLookup.GetValueOrDefault(p.CompanyId),
            p.Code,
            p.Name,
            p.Address,
            buildingCountLookup.GetValueOrDefault(p.Id, 0),
            p.CurrentStatus.ToString(),
            p.CreatedAt
        )).ToList();

        return new PagedResult<PropertyListItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = normalized.Page,
            PageSize = normalized.PageSize
        };
    }

    private static IQueryable<PropertyAggregate> ApplySorting(
        IQueryable<PropertyAggregate> query,
        SortQuery sortQuery)
    {
        if (string.IsNullOrWhiteSpace(sortQuery.Field))
            return query.OrderByDescending(p => p.CreatedAt);

        return sortQuery.Field.ToLowerInvariant() switch
        {
            "code" => sortQuery.IsDescending
                ? query.OrderByDescending(p => p.Code)
                : query.OrderBy(p => p.Code),
            "name" => sortQuery.IsDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "address" => sortQuery.IsDescending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            "created" or "createdat" => sortQuery.IsDescending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }
}
