import { Component, ChangeDetectionStrategy, input, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { PropertyService } from '../services/property.service';
import { PropertyAuditLogEntry } from '../models/property.models';
import { TimelineComponent, TimelineEntry } from '../../../../shared/components/timeline';
import { UserService, UserDetails } from '../../../core/services/user.service';

@Component({
  selector: 'app-property-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineComponent],
  template: `
    <app-timeline
      [entries]="timelineEntries()"
      [loading]="loading()"
      [emptyIcon]="'history'"
      [emptyTitle]="t.translate('properties.noAuditLogs')"
      [emptyDescription]="t.translate('properties.noAuditLogsDescription')" />
  `,
})
export class PropertyAuditLogComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  protected readonly t = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserService);

  readonly propertyId = input.required<string>();

  readonly loading = signal(false);
  private readonly entries = signal<PropertyAuditLogEntry[]>([]);
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

    this.propertyService.getAuditLog(this.propertyId()).pipe(
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

  private loadUserDetails(entries: PropertyAuditLogEntry[]): void {
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
    const key = `properties.events.${normalized.toLowerCase()}`;
    const translated = this.t.translate(key);
    return translated === key ? normalized : translated;
  }

  private formatEventData(entry: PropertyAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = entry.data as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.t.translate('properties.code')}: ${data['code'] || ''}, ${this.t.translate('properties.name')}: ${data['name'] || ''}`;
      case 'codeupdated':
        if (data['oldCode'] && data['newCode']) {
          return `${data['oldCode']} → ${data['newCode']}`;
        }
        return `${this.t.translate('properties.newCode')}: ${data['newCode'] || ''}`;
      case 'nameupdated':
        if (data['oldName'] && data['newName']) {
          return `${data['oldName']} → ${data['newName']}`;
        }
        return `${this.t.translate('properties.newName')}: ${data['newName'] || ''}`;
      case 'addressupdated':
        if (data['oldAddress'] && data['newAddress']) {
          return `${data['oldAddress']} → ${data['newAddress']}`;
        }
        return `${this.t.translate('properties.newAddress')}: ${data['newAddress'] || ''}`;
      case 'deleted':
        return this.t.translate('properties.propertyDeleted');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
