using Microsoft.AspNetCore.Authorization;
using ProperTea.Company.Features.Companies.Lifecycle;
using ProperTea.Infrastructure.Common.Auth;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Company.Features.Companies;

public static class CompanyEndpoints
{
    [WolverinePost("/companies")]
    [Authorize]
    public static async Task<IResult> CreateCompany(
        CreateCompanyRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var companyId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new CreateCompany(request.Name));

        return Results.Created($"/companies/{companyId}", new { Id = companyId });
    }

    [WolverineGet("/companies")]
    [Authorize]
    public static async Task<List<CompanyResponse>> ListCompanies(
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        return await bus.InvokeForTenantAsync<List<CompanyResponse>>(
            tenantId,
            new ListCompanies());
    }

    [WolverineGet("/companies/{id}")]
    [Authorize]
    public static async Task<IResult> GetCompany(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var company = await bus.InvokeForTenantAsync<CompanyResponse?>(
            tenantId,
            new GetCompany(id));

        return company == null ? Results.NotFound() : Results.Ok(company);
    }

    [WolverinePut("/companies/{id}")]
    [Authorize]
    public static async Task<IResult> UpdateCompanyName(
        Guid id,
        UpdateCompanyNameRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateCompanyName(id, request.Name));

        return Results.NoContent();
    }

    [WolverineDelete("/companies/{id}")]
    [Authorize]
    public static async Task<IResult> DeleteCompany(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new DeleteCompany(id));

        return Results.NoContent();
    }
}

public record CreateCompanyRequest(string Name);
public record UpdateCompanyNameRequest(string Name);
