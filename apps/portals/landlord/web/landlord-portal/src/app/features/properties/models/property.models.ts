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
  squareFootage: number | null;
  buildingCount: number;
  status: string;
  createdAt: Date;
}

export interface BuildingResponse {
  id: string;
  code: string;
  name: string;
}

export interface PropertyDetailResponse {
  id: string;
  companyId: string;
  code: string;
  name: string;
  address: string;
  squareFootage: number | null;
  buildings: BuildingResponse[];
  status: string;
  createdAt: Date;
}

export type PagedPropertiesResponse = PagedResult<PropertyListItem>;

export interface CreatePropertyRequest {
  companyId: string;
  code: string;
  name: string;
  address: string;
  squareFootage?: number;
}

export interface UpdatePropertyRequest {
  code: string;
  name: string;
  address: string;
  squareFootage?: number;
}

export interface PropertySelectItem {
  id: string;
  name: string;
}
