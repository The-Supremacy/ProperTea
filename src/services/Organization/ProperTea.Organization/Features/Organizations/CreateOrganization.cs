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
        OrganizationService service,
        IMessageBus bus)
    {
        var organization = await service.CreateNewOrganizationAsync(command.Name).ConfigureAwait(false);

        await dbContext.Organizations.AddAsync(organization).ConfigureAwait(false);

        return new OrganizationCreated(organization.Id, organization.Name, organization.Alias);
    }
}
