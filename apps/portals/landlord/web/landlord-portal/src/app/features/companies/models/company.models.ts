import { PagedResult } from '../../../../shared/components/entity-list-view';

export interface CompanyFilters {
  name?: string;
  code?: string;
}

export interface CreateCompanyRequest {
  code: string;
  name: string;
}

export interface UpdateCompanyRequest {
  code?: string;
  name: string;
}

export interface CompanyListItem {
  id: string;
  code: string;
  name: string;
  status: string;
  createdAt: Date;
}

export interface CompanyDetailResponse {
  id: string;
  code: string;
  name: string;
  status: string;
  createdAt: Date;
}

export type PagedCompaniesResponse = PagedResult<CompanyListItem>;

export interface CheckNameResponse {
  available: boolean;
  existingCompanyId?: string;
}

export interface CheckCodeResponse {
  available: boolean;
  existingCompanyId?: string;
}

export interface CompanyAuditLogEntry {
  eventType: string;
  timestamp: Date;
  username?: string;
  version: number;
  data: any;
}

export interface CompanyAuditLogResponse {
  companyId: string;
  entries: CompanyAuditLogEntry[];
}

export interface CompanySelectItem {
  id: string;
  code: string;
  name: string;
}
