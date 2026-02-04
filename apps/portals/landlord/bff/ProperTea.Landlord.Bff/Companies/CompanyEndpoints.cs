using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Companies;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .RequireAuthorization()
            .WithTags("Companies");

        _ = group.MapPost("/", CreateCompany)
            .WithName("CreateCompany");

        _ = group.MapGet("/", GetCompanies)
            .WithName("GetCompanies");

        _ = group.MapGet("/check-name", CheckCompanyName)
            .WithName("CheckCompanyName");

        _ = group.MapGet("/{id:guid}", GetCompany)
            .WithName("GetCompany");

        _ = group.MapPut("/{id:guid}", UpdateCompanyName)
            .WithName("UpdateCompanyName");

        _ = group.MapDelete("/{id:guid}", DeleteCompany)
            .WithName("DeleteCompany");

        return app;
    }

    private static async Task<IResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        [FromServices] CompanyClient client,
        CancellationToken ct)
    {
        var company = await client.CreateCompanyAsync(request, ct);
        return Results.Created($"/api/companies/{company.Id}", company);
    }

    private static async Task<IResult> GetCompanies(
        [FromServices] CompanyClient client,
        [AsParameters] ListCompaniesQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var companies = await client.GetCompaniesAsync(query, pagination, sort, ct);
        return Results.Ok(companies);
    }

    private static async Task<IResult> CheckCompanyName(
        [FromServices] CompanyClient client,
        [FromQuery] string name,
        [FromQuery] Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var result = await client.CheckCompanyNameAsync(name, excludeId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCompany(
        Guid id,
        [FromServices] CompanyClient client,
        CancellationToken ct)
    {
        var company = await client.GetCompanyAsync(id, ct);
        return company is null ? Results.NotFound() : Results.Ok(company);
    }

    private static async Task<IResult> UpdateCompanyName(
        Guid id,
        [FromBody] UpdateCompanyNameRequest request,
        [FromServices] CompanyClient client,
        CancellationToken ct)
    {
        await client.UpdateCompanyNameAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCompany(
        Guid id,
        [FromServices] CompanyClient client,
        CancellationToken ct)
    {
        await client.DeleteCompanyAsync(id, ct);
        return Results.NoContent();
    }
}
