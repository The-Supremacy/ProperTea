import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, firstValueFrom } from 'rxjs';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OrganizationService } from '../services/organization.service';
import { OrganizationDetailResponse } from '../models/organization.models';
import { SessionService } from '../../../core/services/session.service';
import { EntityDetailsViewComponent, EntityDetailsConfig } from '../../../../shared/components/entity-details-view';
import { Tabs, TabPanel, TabList, Tab, TabContent } from '@angular/aria/tabs';
import { OrganizationAuditLogComponent } from '../audit-log/organization-audit-log.component';
import { SpinnerComponent } from '../../../../shared/components/spinner';
import { StatusBadgeDirective } from '../../../../shared/directives';

@Component({
  selector: 'app-organization-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    Tabs,
    TabList,
    Tab,
    TabPanel,
    TabContent,
    OrganizationAuditLogComponent,
    SpinnerComponent,
    StatusBadgeDirective
  ],
  templateUrl: './organization-details.component.html',
  styleUrl: './organization-details.component.css'
})
export class OrganizationDetailsComponent implements OnInit {
  private router = inject(Router);
  private organizationService = inject(OrganizationService);
  private sessionService = inject(SessionService);
  private translocoService = inject(TranslocoService);

  // State
  organization = signal<OrganizationDetailResponse | null>(null);
  loading = signal(false);
  selectedTab = signal<string>('details');

  // Get external organization ID from token (via session)
  externalOrganizationId = computed(() => this.sessionService.context()?.externalOrganizationId ?? '');
  organizationName = computed(() => this.sessionService.organizationName());

  // Internal organization ID from fetched organization (for audit log)
  internalOrganizationId = computed(() => this.organization()?.id ?? '');

  // Details view configuration
  detailsConfig = computed<EntityDetailsConfig>(() => ({
    title: this.translocoService.translate('organizations.detailsTitle'),
    subtitle: this.organizationName(),
    showBackButton: true,
    showRefresh: true,
    primaryActions: [],
    secondaryActions: [],
  }));

  ngOnInit(): void {
    const externalOrgId = this.externalOrganizationId();
    if (!externalOrgId) {
      this.router.navigate(['/']);
      return;
    }

    this.loadOrganization(externalOrgId);
  }

  protected loadOrganization(externalOrgId: string): void {
    this.loading.set(true);

    this.organizationService.getOrganizationByExternalId(externalOrgId).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (organization) => {
        if (organization) {
          this.organization.set(organization);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: () => {
        this.router.navigate(['/']);
      }
    });
  }

  async refresh(): Promise<void> {
    const externalOrgId = this.externalOrganizationId();
    if (!externalOrgId) return;
    await firstValueFrom(this.organizationService.getOrganizationByExternalId(externalOrgId).pipe(
      finalize(() => this.loading.set(false))
    )).then((organization) => {
      if (organization) {
        this.organization.set(organization);
      }
    });
  }
}
