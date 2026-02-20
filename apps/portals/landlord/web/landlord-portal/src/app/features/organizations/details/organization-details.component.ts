import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, firstValueFrom } from 'rxjs';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OrganizationService } from '../services/organization.service';
import { OrganizationDetailResponse } from '../models/organization.models';
import { SessionService } from '../../../core/services/session.service';
import { EntityDetailsViewComponent, EntityDetailsConfig } from '../../../../shared/components/entity-details-view';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';
import { OrganizationAuditLogComponent } from '../audit-log/organization-audit-log.component';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { StatusBadgeDirective } from '../../../../shared/directives';

@Component({
  selector: 'app-organization-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    HlmTabsImports,
    OrganizationAuditLogComponent,
    HlmSpinner,
    HlmCardImports,
    HlmLabel,
    HlmBadgeImports,
    StatusBadgeDirective
  ],
  templateUrl: './organization-details.component.html'
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

  organizationId = computed(() => this.sessionService.context()?.organizationId ?? '');
  organizationName = computed(() => this.sessionService.organizationName());

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
    const orgId = this.organizationId();
    if (!orgId) {
      this.router.navigate(['/']);
      return;
    }

    this.loadOrganization(orgId);
  }

  protected loadOrganization(organizationId: string): void {
    this.loading.set(true);

    this.organizationService.getOrganization(organizationId).pipe(
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
    const orgId = this.organizationId();
    if (!orgId) return;
    await firstValueFrom(this.organizationService.getOrganization(orgId).pipe(
      finalize(() => this.loading.set(false))
    )).then((organization) => {
      if (organization) {
        this.organization.set(organization);
      }
    });
  }
}
