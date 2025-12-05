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

public record LocalOrganizationCreated(Guid OrganizationId, Guid CreatorUserId);

public static class CreateOrganizationHandler
{
    public static async Task<LocalOrganizationCreated> HandleAsync(
        CreateOrganization command,
        OrganizationDbContext dbContext,
        OrganizationDomainService domainService)
    {
        var organization = await domainService.CreateNewOrganizationAsync(command.Name).ConfigureAwait(false);
        await dbContext.Organizations.AddAsync(organization).ConfigureAwait(false);

        return new LocalOrganizationCreated(organization.Id, command.CreatorUserId);
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

                    var result = await bus.InvokeAsync<OrganizationProvisioned>(command).ConfigureAwait(false);
                    return Results.Created($"/api/v1/organizations/{result.OrganizationId}", result);
                })
            .WithTags("Organizations")
            .WithName("CreateOrganization");
    }
}
