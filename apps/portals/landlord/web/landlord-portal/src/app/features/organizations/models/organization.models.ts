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
