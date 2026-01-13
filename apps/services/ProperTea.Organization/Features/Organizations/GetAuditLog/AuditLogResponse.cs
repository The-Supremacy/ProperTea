namespace ProperTea.Organization.Features.Organizations.GetAuditLog;

/// <summary>
/// Base audit log entry. Frontend translates eventType into human-readable text.
/// </summary>
public record AuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? UserId,
    int Version,
    object Data // Event-specific data for interpolation
);

/// <summary>
/// Event-specific data objects for frontend interpolation
/// </summary>
public static class AuditEventData
{
    public record OrganizationCreated(string Name, string Slug);

    public record ZitadelLinked(string ZitadelOrganizationId);

    public record OrganizationActivated;

    public record NameChanged(string OldName, string NewName);

    public record SlugChanged(string OldSlug, string NewSlug);

    public record OrganizationDeactivated(string Reason);
}

public record AuditLogResponse(
    Guid OrganizationId,
    IReadOnlyList<AuditLogEntry> Entries
);
