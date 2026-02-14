import { Component, ChangeDetectionStrategy, input, signal, OnInit, inject, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CompanyService } from '../services/company.service';
import { CompanyAuditLogEntry } from '../models/company.models';
import { SpinnerComponent } from '../../../../shared/components/spinner';
import { IconComponent } from '../../../../shared/components/icon';
import { UserService, UserDetails } from '../../../core/services/user.service';

@Component({
  selector: 'app-company-audit-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslocoPipe, SpinnerComponent, IconComponent],
  template: `
    <div class="space-y-4">
      <!-- Loading State -->
      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <app-spinner size="lg" />
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && entries().length === 0) {
        <div class="text-center py-12">
          <app-icon name="history" [size]="48" class="mx-auto text-muted-foreground mb-4" />
          <h3 class="text-lg font-semibold mb-2">{{ 'companies.noAuditLogs' | transloco }}</h3>
          <p class="text-sm text-muted-foreground">{{ 'companies.noAuditLogsDescription' | transloco }}</p>
        </div>
      }

      <!-- Timeline -->
      @if (!loading() && entries().length > 0) {
        <div class="relative space-y-6 pl-8">
          <!-- Vertical line -->
          <div class="absolute left-2 top-0 bottom-0 w-px bg-border"></div>

          @for (entry of entries(); track entry.version) {
            <div class="relative">
              <!-- Dot on timeline -->
              <div class="absolute -left-8 top-1.5 w-3 h-3 rounded-full bg-primary border-2 border-background"></div>

              <!-- Event Card -->
              <div class="rounded-lg border bg-card p-4 shadow-sm">
                <div class="flex items-start justify-between mb-2">
                  <div>
                    <h4 class="font-semibold text-sm">{{ getEventLabel(entry.eventType) }}</h4>
                    <p class="text-xs text-muted-foreground mt-1">
                      {{ entry.timestamp | date: 'medium' }}
                      @if (entry.username) {
                        <span class="ml-2">• {{ getUserDisplayName(entry.username) }}</span>
                      }
                    </p>
                  </div>
                  <span class="text-xs font-mono text-muted-foreground">v{{ entry.version }}</span>
                </div>

                <!-- Event-specific data -->
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
  styles: []
})
export class CompanyAuditLogComponent implements OnInit {
  private companyService = inject(CompanyService);
  private translocoService = inject(TranslocoService);
  private userService = inject(UserService);

  companyId = input.required<string>();

  loading = signal(false);
  entries = signal<CompanyAuditLogEntry[]>([]);
  userDetailsMap = signal(new Map<string, UserDetails>());

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
        // Fetch user details for all unique usernames
        this.loadUserDetails(response.entries);
      },
      error: (error) => {
        console.error('Failed to load audit log:', error);
        this.entries.set([]);
      }
    });
  }

  private loadUserDetails(entries: CompanyAuditLogEntry[]): void {
    const usernames = [...new Set(entries.map(e => e.username).filter((u): u is string => !!u))];

    usernames.forEach(userId => {
      this.userService.getUserDetails(userId).subscribe(userDetails => {
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

  protected getUserDisplayName(userId: string): string {
    const userDetails = this.userDetailsMap().get(userId);
    return userDetails?.displayName ?? userId.substring(0, 8);
  }

  private normalizeEventType(eventType: string): string {
    // Strip namespace and version: "company.created.v1" -> "created"
    const parts = eventType.split('.');
    const typeName = parts.length > 1 ? parts[parts.length - 2] : eventType;
    return typeName.replace(/-/g, '');
  }

  protected getEventLabel(eventType: string): string {
    const normalized = this.normalizeEventType(eventType);
    const key = `companies.events.${normalized.toLowerCase()}`;
    const translated = this.translocoService.translate(key);
    return translated === key ? normalized : translated;
  }

  protected formatEventData(entry: CompanyAuditLogEntry): string {
    const normalized = this.normalizeEventType(entry.eventType);
    const data = entry.data as any;

    switch (normalized.toLowerCase()) {
      case 'created':
        return `${this.translocoService.translate('companies.code')}: ${data.code || ''}, ${this.translocoService.translate('companies.name')}: ${data.name || ''}`;
      case 'codeupdated':
        if (data.oldCode && data.newCode) {
          return `${data.oldCode} → ${data.newCode}`;
        }
        return `${this.translocoService.translate('companies.newCode')}: ${data.newCode || ''}`;
      case 'nameupdated':
        if (data.oldName && data.newName) {
          return `${data.oldName} → ${data.newName}`;
        }
        return `${this.translocoService.translate('companies.newName')}: ${data.newName || ''}`;
      case 'deleted':
        return this.translocoService.translate('companies.companyDeleted');
      default:
        // Format any other data gracefully
        return Object.entries(data)
          .map(([key, value]) => `${key}: ${value}`)
          .join(', ');
    }
  }
}
