import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  RegisterOrganizationRequest,
  RegisterOrganizationResponse,
  CheckAvailabilityResponse,
  OrganizationAuditLogResponse
} from '../models/organization.models';

@Injectable({
  providedIn: 'root',
})
export class OrganizationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/organizations';

  registerOrganization(
    request: RegisterOrganizationRequest,
  ): Observable<RegisterOrganizationResponse> {
    return this.http.post<RegisterOrganizationResponse>(this.baseUrl, request);
  }

  checkAvailability(name: string): Observable<CheckAvailabilityResponse> {
    const params = new HttpParams().set('name', name);
    return this.http.get<CheckAvailabilityResponse>(`${this.baseUrl}/check-availability`, {
      params,
    });
  }
}
