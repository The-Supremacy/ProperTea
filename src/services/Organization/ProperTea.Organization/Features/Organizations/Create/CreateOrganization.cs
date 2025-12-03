using FluentValidation;
using ProperTea.Core.Auth;
using ProperTea.Organization.Domain;
using ProperTea.Organization.Persistence;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Create;

public record CreateOrganization(string Name, Guid CreatorUserId);

public class CreateOrganizationValidator : AbstractValidator<CreateOrganization>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
    }
}

public static class CreateOrganizationHandler
{
    public static async Task<StartOrganizationProvisioning> HandleAsync(
        CreateOrganization command,
        OrganizationDbContext dbContext,
        OrganizationService service)
    {
        var organization = await service.CreateNewOrganizationAsync(command.Name).ConfigureAwait(false);
        await dbContext.Organizations.AddAsync(organization).ConfigureAwait(false);

        return new StartOrganizationProvisioning(organization.Id, command.CreatorUserId);
    }
}

public record CreateOrganizationRequest(string Name);
public static class CreateOrganizationEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/organizations",
                async (CreateOrganizationRequest request, IMessageBus bus, ICurrentUser currentUser) =>
                {
                    if (!currentUser.IsAuthenticated || currentUser.Id is null)
                    {
                        return Results.Unauthorized();
                    }

                    var command = new CreateOrganization(request.Name, Guid.Parse(currentUser.Id!));

                    var createdEvent = await bus.InvokeAsync<OrganizationProvisioned>(command).ConfigureAwait(false);
                    return Results.Created($"/api/v1/organizations/{createdEvent.OrganizationId}", createdEvent);
                })
            .WithTags("Organizations")
            .WithName("CreateOrganization");
    }
}
