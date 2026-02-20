using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Properties;

public static class PropertyIntegrationEvents
{
    [MessageIdentity("properties.created.v1")]
    public class PropertyCreated : IPropertyCreated
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        IAddressData IPropertyCreated.Address => Address;
    }

    [MessageIdentity("properties.updated.v1")]
    public class PropertyUpdated : IPropertyUpdated
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public DateTimeOffset UpdatedAt { get; set; }
        IAddressData IPropertyUpdated.Address => Address;
    }

    [MessageIdentity("properties.deleted.v1")]
    public class PropertyDeleted : IPropertyDeleted
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public DateTimeOffset DeletedAt { get; set; }
    }
}
