import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { SkeletonModule } from 'primeng/skeleton';
import { OrganizationsService } from '../../services/organizations.service';
import { Organization } from '../../models/organization.model';

/**
 * Smart component - Organization detail page
 * Displays full organization information
 */
@Component({
  selector: 'app-organization-detail',
  imports: [
    RouterLink,
    CardModule,
    ButtonModule,
    TagModule,
    DividerModule,
    SkeletonModule
  ],
  templateUrl: './organization-detail.component.html',
  styleUrl: './organization-detail.component.scss'
})
export class OrganizationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly organizationsService = inject(OrganizationsService);

  protected readonly organization = signal<Organization | null>(null);
  protected readonly isLoading = computed(() =>
    this.organizationsService.isLoading()
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrganization(id);
    }
  }

  async loadOrganization(id: string): Promise<void> {
    try {
      const org = await this.organizationsService.getOrganization(id);
      this.organization.set(org);
    } catch (error) {
      console.error('Failed to load organization:', error);
      // TODO: Show error notification and navigate back
    }
  }

  async onDelete(): Promise<void> {
    const org = this.organization();
    if (!org) return;

    // TODO: Add confirmation dialog
    try {
      await this.organizationsService.deleteOrganization(org.id);
      // TODO: Show success notification
      this.router.navigate(['/organizations']);
    } catch (error) {
      console.error('Failed to delete organization:', error);
      // TODO: Show error notification
    }
  }

  goBack(): void {
    this.router.navigate(['/organizations']);
  }
}
