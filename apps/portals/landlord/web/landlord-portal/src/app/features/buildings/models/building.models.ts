import { PagedResult } from '../../../../shared/components/entity-list-view';

export interface BuildingAddress {
  country: string;
  city: string;
  zipCode: string;
  streetAddress: string;
}

export interface BuildingFilters {
  code?: string;
  name?: string;
  propertyId?: string;
}

export interface BuildingListItem {
  id: string;
  propertyId: string;
  code: string;
  name: string;
  status: string;
  createdAt: Date;
}

export interface BuildingEntrance {
  id: string;
  code: string;
  name: string;
}

export interface BuildingDetailResponse {
  id: string;
  propertyId: string;
  code: string;
  name: string;
  address?: BuildingAddress;
  status: string;
  createdAt: Date;
  entrances: BuildingEntrance[];
}

export type PagedBuildingsResponse = PagedResult<BuildingListItem>;

export interface CreateBuildingRequest {
  code: string;
  name: string;
  address?: BuildingAddress;
}

export interface UpdateBuildingRequest {
  code?: string;
  name?: string;
  address?: BuildingAddress;
}

export interface AddEntranceRequest {
  code: string;
  name: string;
}

export interface BuildingSelectItem {
  id: string;
  code: string;
  name: string;
}

export interface BuildingAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: unknown;
}

export interface BuildingAuditLogResponse {
  buildingId: string;
  entries: BuildingAuditLogEntry[];
}
