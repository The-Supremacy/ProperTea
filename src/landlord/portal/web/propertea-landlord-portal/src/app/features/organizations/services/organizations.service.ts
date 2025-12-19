import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  Organization,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationListResponse
} from '../models/organization.model';
import { firstValueFrom } from 'rxjs';

/**
 * Service for managing organizations.
 * Uses HttpClient instead of fetch for Angular features like interceptors.
 */
@Injectable({
  providedIn: 'root'
})
export class OrganizationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/organizations';

  // Signal for tracking loading state
  readonly isLoading = signal(false);

  /**
   * Get paginated list of organizations
   */
  async getOrganizations(page = 1, pageSize = 20): Promise<OrganizationListResponse> {
    this.isLoading.set(true);
    try {
      const params = new HttpParams()
        .set('page', page.toString())
        .set('pageSize', pageSize.toString());

      return await firstValueFrom(
        this.http.get<OrganizationListResponse>(this.baseUrl, { params })
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Get single organization by ID
   */
  async getOrganization(id: string): Promise<Organization> {
    this.isLoading.set(true);
    try {
      return await firstValueFrom(
        this.http.get<Organization>(`${this.baseUrl}/${id}`)
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Create new organization
   */
  async createOrganization(dto: CreateOrganizationDto): Promise<Organization> {
    this.isLoading.set(true);
    try {
      return await firstValueFrom(
        this.http.post<Organization>(this.baseUrl, dto)
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Update existing organization
   */
  async updateOrganization(id: string, dto: UpdateOrganizationDto): Promise<Organization> {
    this.isLoading.set(true);
    try {
      return await firstValueFrom(
        this.http.put<Organization>(`${this.baseUrl}/${id}`, dto)
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Delete organization
   */
  async deleteOrganization(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      await firstValueFrom(
        this.http.delete<void>(`${this.baseUrl}/${id}`)
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
