import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginationQuery, SortQuery } from '../../../../shared/components/entity-list-view';
import {
  CreateUnitRequest,
  PagedUnitsResponse,
  UnitAuditLogResponse,
  UnitDetailResponse,
  UnitFilters,
  UnitSelectItem,
  UpdateUnitRequest,
} from '../models/unit.models';

@Injectable({
  providedIn: 'root',
})
export class UnitService {
  private http = inject(HttpClient);

  list(
    filters: UnitFilters,
    pagination: PaginationQuery,
    sort?: SortQuery,
  ): Observable<PagedUnitsResponse> {
    let params = new HttpParams()
      .set('page', pagination.page.toString())
      .set('pageSize', pagination.pageSize.toString());

    if (filters.propertyId) {
      params = params.set('propertyId', filters.propertyId);
    }

    if (filters.buildingId) {
      params = params.set('buildingId', filters.buildingId);
    }

    if (filters.code) {
      params = params.set('code', filters.code);
    }

    if (filters.unitReference) {
      params = params.set('unitReference', filters.unitReference);
    }

    if (filters.category) {
      params = params.set('category', filters.category);
    }

    if (filters.floor != null) {
      params = params.set('floor', filters.floor.toString());
    }

    if (sort?.field) {
      const sortValue = sort.descending ? `${sort.field}:desc` : sort.field;
      params = params.set('sort', sortValue);
    }

    return this.http.get<PagedUnitsResponse>('/api/units', { params });
  }

  select(propertyId: string): Observable<UnitSelectItem[]> {
    return this.http.get<UnitSelectItem[]>(`/api/properties/${propertyId}/units/select`);
  }

  get(id: string): Observable<UnitDetailResponse | null> {
    return this.http.get<UnitDetailResponse | null>(`/api/units/${id}`);
  }

  create(request: CreateUnitRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/units', request);
  }

  update(id: string, request: UpdateUnitRequest): Observable<void> {
    return this.http.put<void>(`/api/units/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/units/${id}`);
  }

  getAuditLog(id: string): Observable<UnitAuditLogResponse> {
    return this.http.get<UnitAuditLogResponse>(`/api/units/${id}/audit-log`);
  }
}
