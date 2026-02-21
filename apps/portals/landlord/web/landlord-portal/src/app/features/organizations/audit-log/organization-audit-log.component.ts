import { Component, ChangeDetectionStrategy, input, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { OrganizationService } from '../services/organization.service';
import { OrganizationAuditLogEntry } from '../models/organization.models';
import { TimelineComponent, TimelineEntry } from '../../../../shared/components/timeline';
import { UserService, UserDetails } from '../../../core/services/user.service';

@Component({
  selector: 'app-organization-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineComponent],
  template: `
    <app-timeline
      [entries]="timelineEntries()"
      [loading]="loading()"
      [emptyIcon]="'history'"
      [emptyTitle]="t.translate('organizations.noAuditLogs')"
      [emptyDescription]="t.translate('organizations.noAuditLogsDescription')" />
  `,
})
export class OrganizationAuditLogComponent implements OnInit {
  private readonly organizationService = inject(OrganizationService);
  protected readonly t = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserService);

  readonly organizationId = input.required<string>();

  readonly loading = signal(false);
  private readonly entries = signal<OrganizationAuditLogEntry[]>([]);
  private readonly userDetailsMap = signal(new Map<string, UserDetails>());

  protected readonly timelineEntries = computed<TimelineEntry[]>(() =>
    this.entries().map((entry) => ({
      id: entry.version,
      label: this.getEventLabel(entry.eventType),
      timestamp: entry.timestamp,
      user: entry.username ? this.getUserDisplayName(entry.username) : undefined,
      version: entry.version,
      description: this.formatEventData(entry),
    })),
  );

  ngOnInit(): void {
    this.loadAuditLog();
  }

  private loadAuditLog(): void {
    this.loading.set(true);

    this.organizationService.getAuditLog(this.organizationId()).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (response) => {
        this.entries.set(response.entries);
        this.loadUserDetails(response.entries);
      },
      error: () => {
        this.entries.set([]);
      }
    });
  }

  private loadUserDetails(entries: OrganizationAuditLogEntry[]): void {
    const usernames = [...new Set(entries.map(e => e.username).filter((u): u is string => !!u))];

    usernames.forEach(userId => {
      this.userService.getUserDetails(userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(userDetails => {
        if (userDetails) {
          this.userDetailsMap.update(map => {
            const newMap = new Map(map);
            newMap.set(userId, userDetails);
            return newMap;
          });
        }
      });
    });
  }

  private getUserDisplayName(userId: string): string {
    const userDetails = this.userDetailsMap().get(userId);
    return userDetails?.displayName ?? userId.substring(0, 8);
  }

  private normalizeEventType(eventType: string): string {
    const parts = eventType.split('.');
    const typeName = parts.length > 1 ? parts[parts.length - 2] : eventType;
    return typeName.replace(/-/g, '');
  }

  private getEventLabel(eventType: string): string {
    const normalized = this.normalizeEventType(eventType);
    const key = `organizations.events.${normalized.toLowerCase()}`;
    const translated = this.t.translate(key);
    return translated === key ? normalized : translated;
  }

  private formatEventData(entry: OrganizationAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = entry.data as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return this.t.translate('organizations.organizationCreated');
      case 'organizationlinked':
        return `${this.t.translate('organizations.externalOrgLinked')}: ${data['organizationId'] || ''}`;
      case 'activated':
        if (data['oldStatus'] && data['newStatus']) {
          return `${data['oldStatus']} → ${data['newStatus']}`;
        }
        return this.t.translate('organizations.organizationActivated');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
