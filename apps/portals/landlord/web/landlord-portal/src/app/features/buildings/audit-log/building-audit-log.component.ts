import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { IconComponent } from '../../../../shared/components/icon';
import { UserDetails, UserService } from '../../../core/services/user.service';
import { BuildingService } from '../services/building.service';
import { BuildingAuditLogEntry } from '../models/building.models';

@Component({
  selector: 'app-building-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslocoPipe, HlmSpinner, IconComponent],
  template: `
    <div class="space-y-4">
      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <hlm-spinner size="lg" />
        </div>
      }

      @if (!loading() && entries().length === 0) {
        <div class="py-12 text-center">
          <app-icon name="history" [size]="48" class="mx-auto mb-4 text-muted-foreground" />
          <h3 class="mb-2 text-lg font-semibold">{{ 'buildings.noAuditLogs' | transloco }}</h3>
          <p class="text-sm text-muted-foreground">{{ 'buildings.noAuditLogsDescription' | transloco }}</p>
        </div>
      }

      @if (!loading() && entries().length > 0) {
        <div class="relative space-y-6 pl-8">
          <div class="absolute bottom-0 left-2 top-0 w-px bg-border"></div>

          @for (entry of entries(); track entry.version) {
            <div class="relative">
              <div class="absolute -left-8 top-1.5 h-3 w-3 rounded-full border-2 border-background bg-primary"></div>

              <div class="rounded-lg border bg-card p-4 shadow-sm">
                <div class="mb-2 flex items-start justify-between">
                  <div>
                    <h4 class="text-sm font-semibold">{{ getEventLabel(entry.eventType) }}</h4>
                    <p class="mt-1 text-xs text-muted-foreground">
                      {{ entry.timestamp | date: 'medium' }}
                      @if (entry.username) {
                        <span class="ml-2">• {{ getUserDisplayName(entry.username) }}</span>
                      }
                    </p>
                  </div>
                  <span class="font-mono text-xs text-muted-foreground">v{{ entry.version }}</span>
                </div>

                <div class="mt-3 text-sm">
                  {{ formatEventData(entry) }}
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class BuildingAuditLogComponent implements OnInit {
  private buildingService = inject(BuildingService);
  private translocoService = inject(TranslocoService);
  private userService = inject(UserService);

  buildingId = input.required<string>();

  loading = signal(false);
  entries = signal<BuildingAuditLogEntry[]>([]);
  userDetailsMap = signal(new Map<string, UserDetails>());

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

  protected getUserDisplayName(userId: string): string {
    const userDetails = this.userDetailsMap().get(userId);
    return userDetails?.displayName ?? userId.substring(0, 8);
  }

  private normalizeEventType(eventType: string): string {
    const parts = eventType.split('.');
    const typeName = parts.length > 1 ? parts[parts.length - 2] : eventType;
    return typeName.replace(/-/g, '');
  }

  protected getEventLabel(eventType: string): string {
    const normalized = this.normalizeEventType(eventType);
    const key = `buildings.events.${normalized.toLowerCase()}`;
    const translated = this.translocoService.translate(key);
    return translated === key ? normalized : translated;
  }

  protected formatEventData(entry: BuildingAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = (entry.data || {}) as Record<string, unknown>;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.translocoService.translate('buildings.code')}: ${data['code'] || ''}, ${this.translocoService.translate('buildings.name')}: ${data['name'] || ''}`;
      case 'codeupdated':
        if (data['oldCode'] && data['newCode']) {
          return `${data['oldCode']} → ${data['newCode']}`;
        }
        return `${this.translocoService.translate('buildings.newCode')}: ${data['newCode'] || ''}`;
      case 'nameupdated':
        if (data['oldName'] && data['newName']) {
          return `${data['oldName']} → ${data['newName']}`;
        }
        return `${this.translocoService.translate('buildings.newName')}: ${data['newName'] || ''}`;
      case 'deleted':
        return this.translocoService.translate('buildings.buildingDeleted');
      default:
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
