using Marten;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Property.Features.Companies;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Lifecycle;

public record CreateProperty(
    Guid CompanyId,
    string Code,
    string Name,
    Address Address);

public class CreatePropertyHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateProperty command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var companyRef = await session.LoadAsync<CompanyReference>(command.CompanyId);
        if (companyRef is null || companyRef.IsDeleted)
            throw new NotFoundException(
                PropertyErrorCodes.PROPERTY_COMPANY_NOT_FOUND,
                "CompanyReference",
                command.CompanyId);

        var codeExists = await session.Query<PropertyAggregate>()
            .Where(p => p.CompanyId == command.CompanyId
                && p.Code == command.Code
                && p.CurrentStatus == PropertyAggregate.Status.Active)
                .AnyAsync();

        if (codeExists)
            throw new ConflictException(
                PropertyErrorCodes.PROPERTY_CODE_ALREADY_EXISTS,
                $"A property with code '{command.Code}' already exists in this company");

        var propertyId = Guid.NewGuid();
        var created = PropertyAggregate.Create(
            propertyId,
            command.CompanyId,
            command.Code,
            command.Name,
            command.Address,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<PropertyAggregate>(propertyId, created);
        await session.SaveChangesAsync();

        var organizationId = session.TenantId;
        await bus.PublishAsync(new PropertyIntegrationEvents.PropertyCreated
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            CompanyId = command.CompanyId,
            Code = command.Code,
            Name = command.Name,
            Address = new Contracts.Events.AddressData(
                command.Address.Country.ToString(),
                command.Address.City,
                command.Address.ZipCode,
                command.Address.StreetAddress),
            CreatedAt = created.CreatedAt
        });

        return propertyId;
    }
}
