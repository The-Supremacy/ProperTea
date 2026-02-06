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

    [WolverineGet("/organizations/{id}/audit-log")]
    public static async Task<AuditLogResponse> GetAuditLog(
        Guid id,
        IMessageBus bus)
    {
        var query = new GetAuditLogQuery(id);
        return await bus.InvokeAsync<AuditLogResponse>(query);
    }

    [WolverineGet("/organizations/check-name")]
    [AllowAnonymous]
    public static async Task<CheckNameResult> CheckName(
        [AsParameters] CheckNameQuery query,
        IMessageBus bus)
    {
        return await bus.InvokeAsync<CheckNameResult>(query);
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
    Guid OrganizationId
);
