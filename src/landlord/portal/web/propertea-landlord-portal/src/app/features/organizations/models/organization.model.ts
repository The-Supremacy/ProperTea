/**
 * Core organization domain model
 */
export interface Organization {
  id: string;
  name: string;
  description?: string;
  address?: Address;
  contactEmail?: string;
  contactPhone?: string;
  createdAt: Date;
  updatedAt: Date;
  isActive: boolean;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

/**
 * DTOs for API operations
 */
export interface CreateOrganizationDto {
  name: string;
  description?: string;
  address?: Address;
  contactEmail?: string;
  contactPhone?: string;
}

export interface UpdateOrganizationDto {
  name?: string;
  description?: string;
  address?: Address;
  contactEmail?: string;
  contactPhone?: string;
  isActive?: boolean;
}

/**
 * List response with pagination
 */
export interface OrganizationListResponse {
  items: Organization[];
  total: number;
  page: number;
  pageSize: number;
}
