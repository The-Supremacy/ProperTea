import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { OrganizationsService } from '../../services/organizations.service';
import { Organization } from '../../models/organization.model';

/**
 * Smart component - Organizations list page
 * Handles data fetching and state management
 */
@Component({
  selector: 'app-organizations-list',
  imports: [
    RouterLink,
    TableModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    TagModule
  ],
  templateUrl: './organizations-list.component.html',
  styleUrl: './organizations-list.component.scss'
})
export class OrganizationsListComponent {
  private readonly organizationsService = inject(OrganizationsService);

  // State signals
  protected readonly organizations = signal<Organization[]>([]);
  protected readonly totalRecords = signal(0);
  protected readonly currentPage = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly searchQuery = signal('');

  // Computed loading state
  protected readonly isLoading = computed(() =>
    this.organizationsService.isLoading()
  );

  // Filtered organizations based on search
  protected readonly filteredOrganizations = computed(() => {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.organizations();

    return this.organizations().filter(org =>
      org.name.toLowerCase().includes(query) ||
      org.description?.toLowerCase().includes(query)
    );
  });

  constructor() {
    // Load initial data
    this.loadOrganizations();
  }

  async loadOrganizations(): Promise<void> {
    try {
      const response = await this.organizationsService.getOrganizations(
        this.currentPage(),
        this.pageSize()
      );

      this.organizations.set(response.items);
      this.totalRecords.set(response.total);
    } catch (error) {
      console.error('Failed to load organizations:', error);
      // TODO: Add proper error handling/notification
    }
  }

  onPageChange(event: any): void {
    this.currentPage.set(event.page + 1);
    this.pageSize.set(event.rows);
    this.loadOrganizations();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
  }

  async onDelete(organization: Organization): Promise<void> {
    // TODO: Add confirmation dialog
    try {
      await this.organizationsService.deleteOrganization(organization.id);
      await this.loadOrganizations();
      // TODO: Show success notification
    } catch (error) {
      console.error('Failed to delete organization:', error);
      // TODO: Show error notification
    }
  }
}
