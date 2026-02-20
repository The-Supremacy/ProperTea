import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { TimelineComponent, TimelineEntry } from '../../../../shared/components/timeline';
import { UserDetails, UserService } from '../../../core/services/user.service';
import { BuildingService } from '../services/building.service';
import { BuildingAuditLogEntry } from '../models/building.models';

@Component({
  selector: 'app-building-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineComponent],
  template: `
    <app-timeline
      [entries]="timelineEntries()"
      [loading]="loading()"
      [emptyIcon]="'history'"
      [emptyTitle]="t.translate('buildings.noAuditLogs')"
      [emptyDescription]="t.translate('buildings.noAuditLogsDescription')" />
  `,
})
export class BuildingAuditLogComponent implements OnInit {
  private readonly buildingService = inject(BuildingService);
  protected readonly t = inject(TranslocoService);
  private readonly userService = inject(UserService);

  readonly buildingId = input.required<string>();

  readonly loading = signal(false);
  private readonly entries = signal<BuildingAuditLogEntry[]>([]);
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

    this.buildingService
      .getAuditLog(this.buildingId())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.entries.set(response.entries);
          this.loadUserDetails(response.entries);
        },
        error: () => {
          this.entries.set([]);
        },
      });
  }

  private loadUserDetails(entries: BuildingAuditLogEntry[]): void {
    const usernames = [...new Set(entries.map((entry) => entry.username).filter((u): u is string => !!u))];

    usernames.forEach((userId) => {
      this.userService.getUserDetails(userId).subscribe((userDetails) => {
        if (userDetails) {
          this.userDetailsMap.update((map) => {
            const nextMap = new Map(map);
            nextMap.set(userId, userDetails);
            return nextMap;
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
    const key = `buildings.events.${normalized.toLowerCase()}`;
    const translated = this.t.translate(key);
    return translated === key ? normalized : translated;
  }

  private formatEventData(entry: BuildingAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = (entry.data || {}) as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.t.translate('buildings.code')}: ${data['code'] || ''}, ${this.t.translate('buildings.name')}: ${data['name'] || ''}`;
      case 'codeupdated':
        if (data['oldCode'] && data['newCode']) {
          return `${data['oldCode']} → ${data['newCode']}`;
        }
        return `${this.t.translate('buildings.newCode')}: ${data['newCode'] || ''}`;
      case 'nameupdated':
        if (data['oldName'] && data['newName']) {
          return `${data['oldName']} → ${data['newName']}`;
        }
        return `${this.t.translate('buildings.newName')}: ${data['newName'] || ''}`;
      case 'deleted':
        return this.t.translate('buildings.buildingDeleted');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
