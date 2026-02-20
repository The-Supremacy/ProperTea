import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { IconComponent } from '../icon';
import { TimelineEntry } from './timeline.models';

@Component({
  selector: 'app-timeline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, HlmSpinner, IconComponent],
  template: `
    <div class="space-y-4">
      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <hlm-spinner size="lg" />
        </div>
      }

      @if (!loading() && entries().length === 0) {
        <div class="py-12 text-center">
          <app-icon [name]="emptyIcon()" [size]="48" class="mx-auto mb-4 text-muted-foreground" />
          <h3 class="mb-2 text-lg font-semibold">{{ emptyTitle() }}</h3>
          <p class="text-sm text-muted-foreground">{{ emptyDescription() }}</p>
        </div>
      }

      @if (!loading() && entries().length > 0) {
        <div>
          @for (entry of entries(); track entry.id) {
            <div class="flex gap-3">
              <!-- Timeline indicator: dot + vertical line -->
              <div class="flex flex-col items-center">
                <div class="w-px flex-1" [class.bg-border]="!$first"></div>
                <div class="h-3 w-3 shrink-0 rounded-full border-2 border-background bg-primary"></div>
                <div class="w-px flex-1" [class.bg-border]="!$last"></div>
              </div>

              <!-- Event card -->
              <div class="flex-1 py-3">
                <div class="rounded-lg border bg-card p-4 shadow-sm">
                  <div class="mb-2 flex items-start justify-between">
                    <div>
                      <h4 class="text-sm font-semibold">{{ entry.label }}</h4>
                      <p class="mt-1 text-xs text-muted-foreground">
                        {{ entry.timestamp | date: 'medium' }}
                        @if (entry.user) {
                          <span class="ml-2">&bull; {{ entry.user }}</span>
                        }
                      </p>
                    </div>
                    <span class="font-mono text-xs text-muted-foreground">v{{ entry.version }}</span>
                  </div>

                  <div class="mt-3 text-sm">
                    {{ entry.description }}
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class TimelineComponent {
  readonly entries = input<TimelineEntry[]>([]);
  readonly loading = input(false);
  readonly emptyIcon = input('history');
  readonly emptyTitle = input('');
  readonly emptyDescription = input('');
}
