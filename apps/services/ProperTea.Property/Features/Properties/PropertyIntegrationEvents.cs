using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Properties;

public static class PropertyIntegrationEvents
{
    [MessageIdentity("properties.created.v1")]
    public class PropertyCreated : IPropertyCreated
    {
        public Guid PropertyId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public decimal? SquareFootage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("properties.updated.v1")]
    public class PropertyUpdated : IPropertyUpdated
    {
        public Guid PropertyId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public decimal? SquareFootage { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [MessageIdentity("properties.deleted.v1")]
    public class PropertyDeleted : IPropertyDeleted
    {
        public Guid PropertyId { get; set; }
        public Guid OrganizationId { get; set; }
        public DateTimeOffset DeletedAt { get; set; }
    }
}
