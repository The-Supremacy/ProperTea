using ProperTea.Property.Features.Companies.Lifecycle;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Property.Features.Companies;

public static class InternalCompanyReferenceEndpoints
{
    [WolverinePost("/internal/references/companies/seed")]
    public static async Task<IResult> SeedCompanyReferences(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SeedCompanyReferencesResult>(
            new SeedCompanyReferences());
        return Results.Ok(result);
    }
}
