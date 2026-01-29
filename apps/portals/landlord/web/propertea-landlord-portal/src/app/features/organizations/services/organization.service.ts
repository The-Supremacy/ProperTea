import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RegisterOrganizationRequest {
  organizationName: string;
  userEmail: string;
  userFirstName: string;
  userLastName: string;
  userPassword: string;
}

export interface RegisterOrganizationResponse {
  organizationId: string;
}

export interface CheckAvailabilityResponse {
  nameAvailable: boolean;
}

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
