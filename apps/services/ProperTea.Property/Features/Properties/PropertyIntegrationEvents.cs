using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Properties;

public record AddressData(string Country, string City, string ZipCode, string StreetAddress) : IAddressData;

public static class PropertyIntegrationEvents
{
    [MessageIdentity("properties.created.v2")]
    public class PropertyCreated : IPropertyCreated
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        IAddressData IPropertyCreated.Address => Address;
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("properties.updated.v2")]
    public class PropertyUpdated : IPropertyUpdated
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        IAddressData IPropertyUpdated.Address => Address;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [MessageIdentity("properties.deleted.v1")]
    public class PropertyDeleted : IPropertyDeleted
    {
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public DateTimeOffset DeletedAt { get; set; }
    }
}
