namespace ProperTea.Landlord.Bff.Organizations;

public record OrganizationDto(
    Guid Id,
    string Name,
    string Status,
    string? ExternalOrganizationId,
    DateTimeOffset CreatedAt,
    int Version
);

public record OrganizationContextDto(
    Guid? LocalOrgId,
    string? ExternalOrganizationId,
    string? OrgName,
    bool IsSynced
);

public record RegisterOrganizationRequest(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string UserPassword);

public record RegisterOrganizationResponse(
    Guid OrganizationId
);

public record CheckNameResponse(
    bool NameAvailable
);

public record AuditLogEntryDto(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data
);

public record AuditLogResponse(
    Guid OrganizationId,
    IReadOnlyList<AuditLogEntryDto> Entries
);
