import { PagedResult } from '../../../../shared/components/entity-list-view';

export interface PropertyFilters {
  name?: string;
  code?: string;
  companyId?: string;
}

export interface PropertyListItem {
  id: string;
  companyId: string;
  companyName: string | null;
  code: string;
  name: string;
  address: string;
  buildingCount: number;
  status: string;
  createdAt: Date;
}

export interface PropertyDetailResponse {
  id: string;
  companyId: string;
  code: string;
  name: string;
  address: string;
  status: string;
  createdAt: Date;
}

export type PagedPropertiesResponse = PagedResult<PropertyListItem>;

export interface CreatePropertyRequest {
  companyId: string;
  code: string;
  name: string;
  address: string;
}

export interface UpdatePropertyRequest {
  code?: string;
  name?: string;
  address?: string;
}

export interface PropertySelectItem {
  id: string;
  name: string;
}

export interface PropertyAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: any;
}

export interface PropertyAuditLogResponse {
  propertyId: string;
  entries: PropertyAuditLogEntry[];
}
