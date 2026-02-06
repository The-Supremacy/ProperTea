import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CheckNameResponse,
  RegisterOrganizationRequest,
  RegisterOrganizationResponse,
} from '../models/organization.models';

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {
  private http = inject(HttpClient);

  checkName(name: string): Observable<CheckNameResponse> {
    return this.http.get<CheckNameResponse>(
      `/api/organizations/check-name?name=${encodeURIComponent(name)}`
    );
  }

  register(request: RegisterOrganizationRequest): Observable<RegisterOrganizationResponse> {
    return this.http.post<RegisterOrganizationResponse>('/api/organizations', request);
  }
}
