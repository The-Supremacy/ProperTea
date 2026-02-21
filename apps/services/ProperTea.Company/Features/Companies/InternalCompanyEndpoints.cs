using ProperTea.Company.Features.Companies.Lifecycle;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Company.Features.Companies;

public static class InternalCompanyEndpoints
{
    [WolverineGet("/internal/companies/snapshot")]
    public static async Task<IResult> GetCompanySnapshot(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<List<CompanySnapshotItem>>(new GetCompanySnapshot());
        return Results.Ok(result);
    }
}
