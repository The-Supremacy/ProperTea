using Marten;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CompanyFilters
{
    public string? Name { get; set; }
}

public record ListCompanies(
    CompanyFilters Filters,
    PaginationQuery Pagination,
    SortQuery Sort);

public class ListCompaniesHandler : IWolverineHandler
{
    public async Task<PagedResult<CompanyResponse>> Handle(
        ListCompanies command,
        IDocumentSession session)
    {
        var normalized = command.Pagination.Normalize();

        var baseQuery = session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active);

        if (!string.IsNullOrWhiteSpace(command.Filters.Name))
        {
            baseQuery = baseQuery.Where(c => c.Name.Contains(command.Filters.Name, StringComparison.OrdinalIgnoreCase));
        }

        baseQuery = ApplySorting(baseQuery, command.Sort);

        var totalCount = await baseQuery.CountAsync();

        var companies = await baseQuery
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync();

        var items = companies.Select(c => new CompanyResponse(
            c.Id,
            c.Name,
            c.CurrentStatus.ToString(),
            c.CreatedAt
        )).ToList();

        return new PagedResult<CompanyResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = normalized.Page,
            PageSize = normalized.PageSize
        };
    }

    private static IQueryable<CompanyAggregate> ApplySorting(
        IQueryable<CompanyAggregate> query,
        SortQuery sortQuery)
    {
        if (string.IsNullOrWhiteSpace(sortQuery.Field))
            return query.OrderByDescending(c => c.CreatedAt);

        return sortQuery.Field.ToLowerInvariant() switch
        {
            "name" => sortQuery.IsDescending
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name),
            "created" or "createdat" => sortQuery.IsDescending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt) // Fallback to default
        };
    }
}
