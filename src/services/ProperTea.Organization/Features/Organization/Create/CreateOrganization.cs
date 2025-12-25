namespace ProperTea.Organization.Features.Organization.Create;

public record CreateOrganization(
    string Name,
    string Slug,
    SubscriptionTier Tier,
    Guid CreatedBy
);

public record OrganizationInitiated(
    Guid OrganizationId,
    string Name,
    string Slug,
    SubscriptionTier Tier,
    Guid CreatedBy,
    DateTime Timestamp
);

public record ZitadelOrganizationCreated(
    Guid OrganizationId,
    Guid ZitadelOrgId,
    DateTime Timestamp
);

public record OrganizationActivated(
    Guid OrganizationId,
    DateTime Timestamp
);

public record OrganizationCreated(
    Guid OrganizationId,
    string Name,
    string Slug,
    Guid ZitadelOrgId
);
