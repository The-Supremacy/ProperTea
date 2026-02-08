import { PagedResult } from '../../../../shared/components/entity-list-view';

export interface CompanyFilters {
  name?: string;
}

export interface CreateCompanyRequest {
  name: string;
}

export interface UpdateCompanyNameRequest {
  name: string;
}

export interface CompanyListItem {
  id: string;
  name: string;
  status: string;
  createdAt: Date;
}

export interface CompanyDetailResponse {
  id: string;
  name: string;
  status: string;
  createdAt: Date;
}

export type PagedCompaniesResponse = PagedResult<CompanyListItem>;

export interface CheckNameResponse {
  available: boolean;
  existingCompanyId?: string;
}
