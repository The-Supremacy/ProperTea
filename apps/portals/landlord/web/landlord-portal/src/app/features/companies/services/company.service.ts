import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginationQuery, SortQuery } from '../../../../shared/components/entity-list-view';
import {
  CompanyFilters,
  CompanyDetailResponse,
  PagedCompaniesResponse,
  CreateCompanyRequest,
  UpdateCompanyRequest,
  CheckNameResponse,
  CompanyAuditLogResponse
} from '../models/company.models';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private http = inject(HttpClient);

  list(
    filters: CompanyFilters,
    pagination: PaginationQuery,
    sort?: SortQuery
  ): Observable<PagedCompaniesResponse> {
    let params = new HttpParams()
      .set('page', pagination.page.toString())
      .set('pageSize', pagination.pageSize.toString());

    if (filters.name) {
      params = params.set('name', filters.name);
    }

    if (sort?.field) {
      const sortValue = sort.descending ? `${sort.field}:desc` : sort.field;
      params = params.set('sort', sortValue);
    }

    return this.http.get<PagedCompaniesResponse>('/api/companies', { params });
  }

  get(id: string): Observable<CompanyDetailResponse | null> {
    return this.http.get<CompanyDetailResponse | null>(`/api/companies/${id}`);
  }

  create(request: CreateCompanyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/companies', request);
  }

  update(id: string, request: UpdateCompanyRequest): Observable<void> {
    return this.http.put<void>(`/api/companies/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/companies/${id}`);
  }

  checkName(name: string, excludeId?: string): Observable<CheckNameResponse> {
    let params = new HttpParams().set('name', name);

    if (excludeId) {
      params = params.set('excludeId', excludeId);
    }

    return this.http.get<CheckNameResponse>('/api/companies/check-name', { params });
  }

  getAuditLog(id: string): Observable<CompanyAuditLogResponse> {
    return this.http.get<CompanyAuditLogResponse>(`/api/companies/${id}/audit-log`);
  }
}
