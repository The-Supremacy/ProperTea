using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record SelectProperties(Guid? CompanyId);

public record SelectItem(Guid Id, string Code, string Name);

public class SelectPropertiesHandler : IWolverineHandler
{
    public async Task<List<SelectItem>> Handle(
        SelectProperties command,
        IDocumentSession session)
    {
        var query = session.Query<PropertyAggregate>()
            .Where(p => p.CurrentStatus == PropertyAggregate.Status.Active);

        if (command.CompanyId.HasValue)
        {
            query = query.Where(p => p.CompanyId == command.CompanyId.Value);
        }

        var properties = await query
            .OrderBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Name })
            .ToListAsync();

        return [.. properties.Select(p => new SelectItem(p.Id, p.Code, p.Name))];
    }
}
