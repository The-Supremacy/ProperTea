namespace ProperTea.Landlord.Bff.Organizations;

/// <summary>
/// Organization DTOs matching backend contracts
/// </summary>
public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string? ZitadelOrganizationId,
    DateTimeOffset CreatedAt,
    int Version
);

public record OrganizationContextDto(
    Guid? LocalOrgId,
    string? ZitadelOrgId,
    string? OrgName,
    bool IsSynced
);

public record CreateOrganizationRequest(
    string Name,
    string Slug
);

public record CreateOrganizationResponse(
    Guid OrganizationId
);

public record UpdateOrganizationRequest(
    string? NewName,
    string? NewSlug
);

public record DeactivateOrganizationRequest(
    string Reason
);

public record CheckAvailabilityResponse(
    bool NameAvailable,
    bool SlugAvailable
);

public record AuditLogEntryDto(
    string EventType,
    DateTimeOffset Timestamp,
    string? UserId,
    int Version,
    object Data
);

public record AuditLogResponse(
    Guid OrganizationId,
    IReadOnlyList<AuditLogEntryDto> Entries
);
