import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal, DestroyRef } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { TimelineComponent, TimelineEntry } from '../../../../shared/components/timeline';
import { UserDetails, UserService } from '../../../core/services/user.service';
import { UnitService } from '../services/unit.service';
import { UnitAuditLogEntry } from '../models/unit.models';

@Component({
  selector: 'app-unit-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineComponent],
  template: `
    <app-timeline
      [entries]="timelineEntries()"
      [loading]="loading()"
      [emptyIcon]="'history'"
      [emptyTitle]="t.translate('units.noAuditLogs')"
      [emptyDescription]="t.translate('units.noAuditLogsDescription')" />
  `,
})
export class UnitAuditLogComponent implements OnInit {
  private readonly unitService = inject(UnitService);
  protected readonly t = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserService);

  readonly unitId = input.required<string>();

  readonly loading = signal(false);
  private readonly entries = signal<UnitAuditLogEntry[]>([]);
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

    this.unitService
      .getAuditLog(this.unitId())
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

  private loadUserDetails(entries: UnitAuditLogEntry[]): void {
    const usernames = [...new Set(entries.map((entry) => entry.username).filter((u): u is string => !!u))];

    usernames.forEach((userId) => {
      this.userService.getUserDetails(userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((userDetails) => {
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
    const key = `units.events.${normalized.toLowerCase()}`;
    const translated = this.t.translate(key);
    return translated === key ? normalized : translated;
  }

  private formatEventData(entry: UnitAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = (entry.data || {}) as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.t.translate('units.unitReference')}: ${data['unitReference'] || ''}, ${this.t.translate('units.category')}: ${data['category'] || ''}`;
      case 'codeupdated':
        return `${data['oldCode'] || '?'} → ${data['newCode'] || '?'}`;
      case 'referenceregenerated':
        return `${data['oldReference'] || '?'} → ${data['newReference'] || '?'}`;
      case 'categorychanged':
        return `${data['oldCategory'] || '?'} → ${data['newCategory'] || '?'}`;
      case 'locationchanged': {
        const parts: string[] = [];
        if (data['oldPropertyId'] !== data['newPropertyId'])
          parts.push(`${this.t.translate('units.property')}: ${String(data['oldPropertyId']).substring(0, 8)} \u2192 ${String(data['newPropertyId']).substring(0, 8)}`);
        if (data['oldBuildingId'] !== data['newBuildingId'])
          parts.push(`${this.t.translate('units.building')}: ${data['newBuildingId'] ? String(data['newBuildingId']).substring(0, 8) : '\u2014'}`);
        return parts.join(', ') || '\u2014';
      }
      case 'addressupdated':
        return `${data['streetAddress'] || ''}, ${data['city'] || ''} ${data['zipCode'] || ''}`.trim();
      case 'floorupdated':
        return `${data['oldFloor'] ?? '—'} → ${data['newFloor'] ?? '—'}`;
      case 'deleted':
        return this.t.translate('units.unitDeleted');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
