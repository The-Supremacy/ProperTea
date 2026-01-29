using ProperTea.Organization.Infrastructure;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public class CheckAvailabilityHandler : IWolverineHandler
{
    public async Task<CheckAvailabilityResult> Handle(
        CheckAvailabilityQuery query,
        IExternalOrganizationClient externalOrgClient,
        CancellationToken ct)
    {
        var nameAvailable = true;

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var exists = await externalOrgClient.CheckOrganizationExistsAsync(query.Name, ct);
            nameAvailable = !exists;
        }

        return new CheckAvailabilityResult(nameAvailable);
    }
}

public record CheckAvailabilityQuery(string? Name);

public record CheckAvailabilityResult(bool NameAvailable);
