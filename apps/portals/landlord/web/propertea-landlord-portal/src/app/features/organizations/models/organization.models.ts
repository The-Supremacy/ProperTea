export interface RegisterOrganizationRequest {
  organizationName: string;
  userEmail: string;
  userFirstName: string;
  userLastName: string;
  userPassword: string;
}

export interface RegisterOrganizationResponse {
  organizationId: string;
}

export interface CheckAvailabilityResponse {
  nameAvailable: boolean;
}

export interface OrganizationAuditLogEntry {
  id: string;
  eventType: string;
  timestamp: Date;
  userId?: string;
  details?: Record<string, unknown>;
}

export interface OrganizationAuditLogResponse {
  organizationId: string;
  entries: OrganizationAuditLogEntry[];
}
