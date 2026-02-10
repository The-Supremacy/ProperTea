export interface CheckNameResponse {
  nameAvailable: boolean;
}

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

export interface OrganizationDetailResponse {
  id: string;
  name?: string;
  status: string;
  tier: string;
  externalOrganizationId?: string;
  createdAt: Date;
}

export interface OrganizationAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: any;
}

export interface OrganizationAuditLogResponse {
  organizationId: string;
  entries: OrganizationAuditLogEntry[];
}
