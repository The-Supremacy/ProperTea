import { PagedResult } from '../../../../shared/components/entity-list-view';

export type UnitCategory = 'Apartment' | 'Commercial' | 'Parking' | 'House' | 'Other';

export const UNIT_CATEGORIES: UnitCategory[] = ['Apartment', 'Commercial', 'Parking', 'House', 'Other'];

export interface UnitAddress {
  country: string;
  city: string;
  zipCode: string;
  streetAddress: string;
}

export interface UnitFilters {
  code?: string;
  unitReference?: string;
  category?: string;
  propertyId?: string;
  buildingId?: string;
  floor?: number;
}

export interface UnitListItem {
  id: string;
  propertyId: string;
  buildingId?: string;
  entranceId?: string;
  code: string;
  unitReference: string;
  category: string;
  address: UnitAddress;
  floor?: number;
  status: string;
  createdAt: Date;
}

export interface UnitDetailResponse {
  id: string;
  propertyId: string;
  buildingId?: string;
  entranceId?: string;
  code: string;
  unitReference: string;
  category: string;
  address: UnitAddress;
  floor?: number;
  status: string;
  createdAt: Date;
}

export type PagedUnitsResponse = PagedResult<UnitListItem>;

export interface CreateUnitRequest {
  propertyId: string;
  buildingId?: string;
  entranceId?: string;
  code: string;
  category: UnitCategory;
  address: UnitAddress;
  floor?: number;
}

export interface UpdateUnitRequest {
  propertyId: string;
  buildingId?: string;
  entranceId?: string;
  code: string;
  category: UnitCategory;
  address: UnitAddress;
  floor?: number;
}

export interface UnitAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: unknown;
}

export interface UnitAuditLogResponse {
  unitId: string;
  entries: UnitAuditLogEntry[];
}

export interface UnitSelectItem {
  id: string;
  code: string;
  unitReference: string;
}
