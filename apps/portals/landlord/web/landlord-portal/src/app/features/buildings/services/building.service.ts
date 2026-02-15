import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginationQuery, SortQuery } from '../../../../shared/components/entity-list-view';
import {
  BuildingAuditLogResponse,
  BuildingDetailResponse,
  BuildingFilters,
  BuildingSelectItem,
  CreateBuildingRequest,
  PagedBuildingsResponse,
  UpdateBuildingRequest,
} from '../models/building.models';

@Injectable({
  providedIn: 'root',
})
export class BuildingService {
  private http = inject(HttpClient);

  list(
    filters: BuildingFilters,
    pagination: PaginationQuery,
    sort?: SortQuery,
  ): Observable<PagedBuildingsResponse> {
    let params = new HttpParams()
      .set('page', pagination.page.toString())
      .set('pageSize', pagination.pageSize.toString());

    if (filters.propertyId) {
      params = params.set('propertyId', filters.propertyId);
    }

    if (filters.name) {
      params = params.set('name', filters.name);
    }

    if (filters.code) {
      params = params.set('code', filters.code);
    }

    if (sort?.field) {
      const sortValue = sort.descending ? `${sort.field}:desc` : sort.field;
      params = params.set('sort', sortValue);
    }

    return this.http.get<PagedBuildingsResponse>(`/api/buildings`, { params });
  }

  select(propertyId: string): Observable<BuildingSelectItem[]> {
    return this.http.get<BuildingSelectItem[]>(`/api/buildings/property/${propertyId}/select`);
  }

  get(id: string): Observable<BuildingDetailResponse | null> {
    return this.http.get<BuildingDetailResponse | null>(`/api/buildings/${id}`);
  }

  create(propertyId: string, request: CreateBuildingRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`/api/buildings/property/${propertyId}`, request);
  }

  update(id: string, request: UpdateBuildingRequest): Observable<void> {
    return this.http.put<void>(`/api/buildings/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/buildings/${id}`);
  }

  getAuditLog(id: string): Observable<BuildingAuditLogResponse> {
    return this.http.get<BuildingAuditLogResponse>(`/api/buildings/${id}/audit-log`);
  }
}
