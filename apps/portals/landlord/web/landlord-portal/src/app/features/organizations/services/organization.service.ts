import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CheckNameResponse,
  RegisterOrganizationRequest,
  RegisterOrganizationResponse,
  OrganizationDetailResponse,
  OrganizationAuditLogResponse,
} from '../models/organization.models';

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {
  private http = inject(HttpClient);

  getOrganization(id: string): Observable<OrganizationDetailResponse> {
    return this.http.get<OrganizationDetailResponse>(`/api/organizations/${id}`);
  }

  getOrganizationByExternalId(externalOrgId: string): Observable<OrganizationDetailResponse> {
    return this.http.get<OrganizationDetailResponse>(`/api/organizations/external/${externalOrgId}`);
  }

  checkName(name: string): Observable<CheckNameResponse> {
    const params = new HttpParams().set('name', name);
    return this.http.get<CheckNameResponse>('/api/organizations_/check-name', { params });
  }

  register(request: RegisterOrganizationRequest): Observable<RegisterOrganizationResponse> {
    return this.http.post<RegisterOrganizationResponse>('/api/organizations', request);
  }

  getAuditLog(id: string): Observable<OrganizationAuditLogResponse> {
    return this.http.get<OrganizationAuditLogResponse>(`/api/organizations/${id}/audit-log`);
  }
}
