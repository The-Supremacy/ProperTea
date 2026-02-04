namespace ProperTea.Infrastructure.Common.Pagination;

public record SortQuery
{
    public string? Sort { get; set; }
    public string? Field => ParseSort().field;
    public string Direction => ParseSort().direction;
    public bool IsDescending => Direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

    private (string? field, string direction) ParseSort()
    {
        if (string.IsNullOrWhiteSpace(Sort))
            return (null, "asc");

        var parts = Sort.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return (null, "asc");

        var field = parts[0].Trim();
        var direction = parts[1].Trim().ToLowerInvariant();

        if (direction is not "asc" and not "desc")
            direction = "asc";

        return (field, direction);
    }
}
