namespace ProperTea.Infrastructure.Common.Pagination;

/// <summary>
/// Base query parameters for pagination
/// </summary>
public record PaginationQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public PaginationQuery Normalize()
    {
        const int minPage = 1;
        const int minPageSize = 10;
        const int maxPageSize = 100;

        return this with
        {
            Page = Math.Max(Page, minPage),
            PageSize = Math.Clamp(PageSize, minPageSize, maxPageSize)
        };
    }
    public int Skip => (Page - 1) * PageSize;

    public int Take => PageSize;
}
