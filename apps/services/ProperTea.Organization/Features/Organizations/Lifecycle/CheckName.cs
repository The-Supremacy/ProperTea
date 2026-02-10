using ProperTea.Organization.Infrastructure;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public class CheckNameHandler : IWolverineHandler
{
    public async Task<CheckNameResult> Handle(
        CheckNameQuery query,
        IExternalOrganizationClient externalOrgClient,
        CancellationToken ct)
    {
        var nameAvailable = true;

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var exists = await externalOrgClient.CheckOrganizationExistsAsync(query.Name, ct);
            nameAvailable = !exists;
        }

        return new CheckNameResult(nameAvailable);
    }
}

public record CheckNameQuery(string? Name = null);

public record CheckNameResult(bool NameAvailable);
