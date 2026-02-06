import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
    const params = new HttpParams().set('name', name);
    return this.http.get<CheckNameResponse>('/api/organizations/check-name', { params });
  }

  register(request: RegisterOrganizationRequest): Observable<RegisterOrganizationResponse> {
    return this.http.post<RegisterOrganizationResponse>('/api/organizations', request);
  }
}
