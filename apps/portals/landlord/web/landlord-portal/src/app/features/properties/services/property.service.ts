import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginationQuery, SortQuery } from '../../../../shared/components/entity-list-view';
import {
  PropertyFilters,
  PropertyDetailResponse,
  PagedPropertiesResponse,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertySelectItem,
} from '../models/property.models';

@Injectable({
  providedIn: 'root',
})
export class PropertyService {
  private http = inject(HttpClient);

  list(
    filters: PropertyFilters,
    pagination: PaginationQuery,
    sort?: SortQuery
  ): Observable<PagedPropertiesResponse> {
    let params = new HttpParams()
      .set('page', pagination.page.toString())
      .set('pageSize', pagination.pageSize.toString());

    if (filters.name) {
      params = params.set('name', filters.name);
    }

    if (filters.code) {
      params = params.set('code', filters.code);
    }

    if (filters.companyId) {
      params = params.set('companyId', filters.companyId);
    }

    if (sort?.field) {
      const sortValue = sort.descending ? `${sort.field}:desc` : sort.field;
      params = params.set('sort', sortValue);
    }

    return this.http.get<PagedPropertiesResponse>('/api/properties', { params });
  }

  select(companyId?: string): Observable<PropertySelectItem[]> {
    let params = new HttpParams();
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    return this.http.get<PropertySelectItem[]>('/api/properties/select', { params });
  }

  get(id: string): Observable<PropertyDetailResponse | null> {
    return this.http.get<PropertyDetailResponse | null>(`/api/properties/${id}`);
  }

  create(request: CreatePropertyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/properties', request);
  }

  update(id: string, request: UpdatePropertyRequest): Observable<void> {
    return this.http.put<void>(`/api/properties/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/properties/${id}`);
  }
}
