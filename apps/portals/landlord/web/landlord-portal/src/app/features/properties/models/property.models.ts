import { PagedResult } from '../../../../shared/components/entity-list-view';

export interface PropertyFilters {
  name?: string;
  code?: string;
  companyId?: string;
}

export interface PropertyAddress {
  country: string;
  city: string;
  zipCode: string;
  streetAddress: string;
}

export function formatAddress(address: PropertyAddress | null | undefined): string {
  if (!address) return '';
  // Legacy snapshots may only have a streetAddress with empty city/zip.
  if (!address.city && !address.zipCode) return address.streetAddress;
  return `${address.streetAddress}, ${address.zipCode} ${address.city}, ${address.country}`;
}

export interface PropertyListItem {
  id: string;
  companyId: string;
  companyName: string | null;
  code: string;
  name: string;
  address: PropertyAddress;
  buildingCount: number;
  status: string;
  createdAt: Date;
}

export interface PropertyDetailResponse {
  id: string;
  companyId: string;
  code: string;
  name: string;
  address: PropertyAddress;
  status: string;
  createdAt: Date;
}

export type PagedPropertiesResponse = PagedResult<PropertyListItem>;

export interface CreatePropertyRequest {
  companyId: string;
  code: string;
  name: string;
  address?: PropertyAddress;
}

export interface UpdatePropertyRequest {
  code?: string;
  name?: string;
  address?: PropertyAddress;
}

export interface PropertySelectItem {
  id: string;
  code: string;
  name: string;
}

export interface PropertyAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: unknown;
}

export interface PropertyAuditLogResponse {
  propertyId: string;
  entries: PropertyAuditLogEntry[];
}
