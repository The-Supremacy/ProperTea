using Marten;
using ProperTea.Property.Features.Buildings;
using ProperTea.Property.Features.Companies;
using ProperTea.Property.Features.Properties;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;

namespace ProperTea.Property.Features.Units.Lifecycle;

public record UnitFilters
{
    public string? UnitNumber { get; set; }
    public string? Code { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? CompanyId { get; set; }
    public UnitCategory? Category { get; set; }
    public int? Floor { get; set; }
}

public record ListUnits(
    UnitFilters Filters,
    PaginationQuery Pagination,
    SortQuery Sort);

public record UnitListItemResponse(
    Guid Id,
    Guid PropertyId,
    string? PropertyName,
    Guid? BuildingId,
    string? BuildingName,
    Guid? CompanyId,
    string? CompanyName,
    string Code,
    string UnitNumber,
    string Category,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount,
    string Status,
    DateTimeOffset CreatedAt);

public class ListUnitsHandler : IWolverineHandler
{
    public async Task<PagedResult<UnitListItemResponse>> Handle(
        ListUnits command,
        IDocumentSession session)
    {
        var normalized = command.Pagination.Normalize();

        var baseQuery = session.Query<UnitAggregate>()
            .Where(u => u.CurrentStatus == UnitAggregate.Status.Active);

        if (!string.IsNullOrWhiteSpace(command.Filters.UnitNumber))
        {
            baseQuery = baseQuery.Where(u =>
                u.UnitNumber.Contains(command.Filters.UnitNumber, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(command.Filters.Code))
        {
            baseQuery = baseQuery.Where(u =>
                u.Code.Contains(command.Filters.Code, StringComparison.OrdinalIgnoreCase));
        }

        if (command.Filters.PropertyId.HasValue)
        {
            baseQuery = baseQuery.Where(u => u.PropertyId == command.Filters.PropertyId.Value);
        }

        if (command.Filters.BuildingId.HasValue)
        {
            baseQuery = baseQuery.Where(u => u.BuildingId == command.Filters.BuildingId.Value);
        }

        if (command.Filters.Category.HasValue)
        {
            baseQuery = baseQuery.Where(u => u.Category == command.Filters.Category.Value);
        }

        if (command.Filters.CompanyId.HasValue)
        {
            var propertyIdsForCompany = await session.Query<PropertyAggregate>()
                .Where(p => p.CompanyId == command.Filters.CompanyId.Value)
                .Select(p => p.Id)
                .ToListAsync();

            baseQuery = baseQuery.Where(u => u.PropertyId.In(propertyIdsForCompany.ToArray()));
        }

        if (command.Filters.Floor.HasValue)
        {
            baseQuery = baseQuery.Where(u => u.Floor == command.Filters.Floor.Value);
        }

        baseQuery = ApplySorting(baseQuery, command.Sort);

        var totalCount = await baseQuery.CountAsync();

        var units = await baseQuery
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync();

        var propertyIds = units.Select(u => u.PropertyId).Distinct().ToList();
        var properties = await session.Query<PropertyAggregate>()
            .Where(p => p.Id.In(propertyIds))
            .ToListAsync();
        var propertyLookup = properties.ToDictionary(p => p.Id);

        var companyIds = properties.Select(p => p.CompanyId).Distinct().ToList();
        var companies = await session.Query<CompanyReference>()
            .Where(c => c.Id.In(companyIds))
            .ToListAsync();
        var companyNameLookup = companies.ToDictionary(c => c.Id, c => c.Name);

        var buildingIds = units
            .Where(u => u.BuildingId.HasValue)
            .Select(u => u.BuildingId!.Value)
            .Distinct()
            .ToList();
        var buildings = await session.Query<BuildingAggregate>()
            .Where(b => b.Id.In(buildingIds) && b.CurrentStatus == BuildingAggregate.Status.Active)
            .ToListAsync();
        var buildingNameLookup = buildings.ToDictionary(b => b.Id, b => b.Name);

        var items = units.Select(u =>
        {
            var prop = propertyLookup.GetValueOrDefault(u.PropertyId);
            var companyName = prop != null && prop.CompanyId != default
                ? companyNameLookup.GetValueOrDefault(prop.CompanyId)
                : null;

            var buildingName = u.BuildingId.HasValue
                ? buildingNameLookup.GetValueOrDefault(u.BuildingId.Value)
                : null;

            return new UnitListItemResponse(
                u.Id,
                u.PropertyId,
                prop?.Name,
                u.BuildingId,
                buildingName,
                prop?.CompanyId != default ? prop?.CompanyId : null,
                companyName,
                u.Code,
                u.UnitNumber,
                u.Category.ToString(),
                u.Floor,
                u.SquareFootage,
                u.RoomCount,
                u.CurrentStatus.ToString(),
                u.CreatedAt
            );
        }).ToList();

        return new PagedResult<UnitListItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = normalized.Page,
            PageSize = normalized.PageSize
        };
    }

    private static IQueryable<UnitAggregate> ApplySorting(
        IQueryable<UnitAggregate> query,
        SortQuery sortQuery)
    {
        if (string.IsNullOrWhiteSpace(sortQuery.Field))
            return query.OrderByDescending(u => u.CreatedAt);

        return sortQuery.Field.ToLowerInvariant() switch
        {
            "code" => sortQuery.IsDescending
                ? query.OrderByDescending(u => u.Code)
                : query.OrderBy(u => u.Code),
            "unitnumber" => sortQuery.IsDescending
                ? query.OrderByDescending(u => u.UnitNumber)
                : query.OrderBy(u => u.UnitNumber),
            "category" => sortQuery.IsDescending
                ? query.OrderByDescending(u => u.Category)
                : query.OrderBy(u => u.Category),
            "floor" => sortQuery.IsDescending
                ? query.OrderByDescending(u => u.Floor)
                : query.OrderBy(u => u.Floor),
            "created" or "createdat" => sortQuery.IsDescending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };
    }
}
