import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CheckAvailabilityResponse {
  nameAvailable: boolean;
  slugAvailable: boolean;
}

export interface CreateOrganizationRequest {
  name: string;
  slug: string;
}

export interface CreateOrganizationResponse {
  organizationId: string;
}

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/organizations';

  checkSlug(slug: string): Observable<boolean> {
    return this.http
      .get<CheckAvailabilityResponse>(`${this.baseUrl}/check-availability?slug=${slug}`)
      .pipe(map((res) => res.slugAvailable));
  }

  async create(req: CreateOrganizationRequest): Promise<CreateOrganizationResponse> {
    return firstValueFrom(this.http.post<CreateOrganizationResponse>(this.baseUrl, req));
  }
}
