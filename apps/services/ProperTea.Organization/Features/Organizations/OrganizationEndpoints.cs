using Microsoft.AspNetCore.Authorization;
using ProperTea.Organization.Features.Organizations.Lifecycle;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEndpoints
{
    [WolverinePost("/organizations")]
    [AllowAnonymous]
    public static async Task<IResult> RegisterOrganization(
        CreateOrganizationRequest request,
        IMessageBus bus)
    {
        var command = new RegisterOrganizationCommand(
            request.OrganizationName,
            request.UserEmail,
            request.UserFirstName,
            request.UserLastName,
            request.UserPassword
        );

        var result = await bus.InvokeAsync<RegistrationResult>(command);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Organization Registration Failed",
                detail: result.Reason,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return Results.Created($"/organizations/{result.OrganizationId}", new CreateOrganizationResult(
            result.OrganizationId
        ));
    }

    [WolverineGet("/organizations_/check-name")]
    [AllowAnonymous]
    public static async Task<CheckNameResult> CheckName(
        [AsParameters] CheckNameQuery query,
        IMessageBus bus)
    {
        return await bus.InvokeAsync<CheckNameResult>(query);
    }

    [WolverineGet("/organizations/{organizationId}")]
    [Authorize]
    public static async Task<IResult> GetOrganization(
        string organizationId,
        IMessageBus bus)
    {
        var query = new GetOrganization(organizationId);
        var result = await bus.InvokeAsync<OrganizationResponse?>(query);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    [WolverineGet("/organizations/{organizationId}/audit-log")]
    public static async Task<AuditLogResponse> GetAuditLog(
        string organizationId,
        IMessageBus bus)
    {
        var query = new GetAuditLogQuery(organizationId);
        return await bus.InvokeAsync<AuditLogResponse>(query);
    }
}

public record CreateOrganizationRequest(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string UserPassword
);

public record CreateOrganizationResult(
    string OrganizationId
);
