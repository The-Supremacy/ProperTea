using Marten;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public class CheckAvailabilityHandler : IWolverineHandler
{
    public async Task<CheckAvailabilityResult> Handle(
        CheckAvailabilityQuery query,
        IQuerySession session,
        CancellationToken ct)
    {
        var nameAvailable = true;
        var slugAvailable = true;

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            nameAvailable = !await session.Query<OrganizationAggregate>()
                .AnyAsync(x => x.Name == query.Name, token: ct);
        }

        if (!string.IsNullOrWhiteSpace(query.Slug))
        {
            slugAvailable = !await session.Query<OrganizationAggregate>()
                .AnyAsync(x => x.Slug == query.Slug, token: ct);
        }

        return new CheckAvailabilityResult(nameAvailable, slugAvailable);
    }
}

public record CheckAvailabilityQuery(string? Name, string? Slug);

public record CheckAvailabilityResult(bool NameAvailable, bool SlugAvailable);
