namespace ProperTea.Landlord.Bff.Organizations;

public record OrganizationDto(
    string OrganizationId,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    int Version
);

public record OrganizationDetailResponse(
    string OrganizationId,
    string? Name,
    string Status,
    string Tier,
    DateTimeOffset CreatedAt
);

public record OrganizationContextDto(
    string? OrganizationId,
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
    string OrganizationId
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
    string OrganizationId,
    IReadOnlyList<AuditLogEntryDto> Entries
);
