import { Component, ChangeDetectionStrategy, input, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { CompanyService } from '../services/company.service';
import { CompanyAuditLogEntry } from '../models/company.models';
import { TimelineComponent, TimelineEntry } from '../../../../shared/components/timeline';
import { UserService, UserDetails } from '../../../core/services/user.service';

@Component({
  selector: 'app-company-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineComponent],
  template: `
    <app-timeline
      [entries]="timelineEntries()"
      [loading]="loading()"
      [emptyIcon]="'history'"
      [emptyTitle]="t.translate('companies.noAuditLogs')"
      [emptyDescription]="t.translate('companies.noAuditLogsDescription')" />
  `,
})
export class CompanyAuditLogComponent implements OnInit {
  private readonly companyService = inject(CompanyService);
  protected readonly t = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserService);

  readonly companyId = input.required<string>();

  readonly loading = signal(false);
  private readonly entries = signal<CompanyAuditLogEntry[]>([]);
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

    this.companyService.getAuditLog(this.companyId()).pipe(
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

  private loadUserDetails(entries: CompanyAuditLogEntry[]): void {
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
    const key = `companies.events.${normalized.toLowerCase()}`;
    const translated = this.t.translate(key);
    return translated === key ? normalized : translated;
  }

  private formatEventData(entry: CompanyAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = entry.data as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.t.translate('companies.code')}: ${data['code'] || ''}, ${this.t.translate('companies.name')}: ${data['name'] || ''}`;
      case 'codeupdated':
        if (data['oldCode'] && data['newCode']) {
          return `${data['oldCode']} → ${data['newCode']}`;
        }
        return `${this.t.translate('companies.newCode')}: ${data['newCode'] || ''}`;
      case 'nameupdated':
        if (data['oldName'] && data['newName']) {
          return `${data['oldName']} → ${data['newName']}`;
        }
        return `${this.t.translate('companies.newName')}: ${data['newName'] || ''}`;
      case 'deleted':
        return this.t.translate('companies.companyDeleted');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
