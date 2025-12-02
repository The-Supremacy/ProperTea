using FluentValidation;
using ProperTea.Organization.Domain;
using ProperTea.Organization.Persistence;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Http;

namespace ProperTea.Organization.Features.Organizations;

public record CreateOrganization(string Name);

[MessageIdentity("organization-created", Version = 1)]
public record OrganizationCreated(Guid OrganizationId, string Name, string Alias);

public class CreateOrganizationValidator : AbstractValidator<CreateOrganization>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
    }
}

public static class CreateOrganizationHandler
{
    public static async Task<OrganizationCreated> HandleAsync(
        CreateOrganization command,
        OrganizationDbContext dbContext,
        OrganizationService service)
    {
        var organization = await service.CreateNewOrganizationAsync(command.Name).ConfigureAwait(false);
        await dbContext.Organizations.AddAsync(organization).ConfigureAwait(false);

        return new OrganizationCreated(organization.Id, organization.Name, organization.Alias);
    }
}

public record OrganizationCreatedResponse(Guid Id) : CreationResponse("/api/v1/organizations/" + Id)
{
    public Guid Id { get; init; } = Id;
}

public static class CreateOrganizationEndpoint
{
    [WolverinePost("/api/v1/organizations")]
    [EndpointSummary("Creates new organization.")]
    public async static Task<(OrganizationCreatedResponse, OrganizationCreated)> PostAsync(CreateOrganization command, IMessageBus messageBus)
    {
        var result = await messageBus.InvokeAsync<OrganizationCreated>(command).ConfigureAwait(false);
        return (
            new OrganizationCreatedResponse(result.OrganizationId),
            result
        );
    }
}
