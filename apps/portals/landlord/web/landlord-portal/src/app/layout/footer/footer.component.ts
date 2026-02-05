import { Component, inject, OnInit } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { HealthService } from '../../core/services/health.service';

@Component({
  selector: 'app-footer',
  imports: [TranslocoPipe],
  template: `
    <footer class="flex h-10 items-center justify-between border-t bg-background px-4 text-xs text-muted-foreground">
      <div>{{ 'footer.copyright' | transloco: {year: currentYear} }}</div>

      <div class="flex items-center gap-2">
        @if (healthService.status().isHealthy) {
          <span class="inline-flex items-center gap-1.5 rounded-full border border-green-500/20 bg-green-500/10 px-2 py-0.5 text-xs font-medium text-green-700 dark:text-green-400">
            <span class="h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse"></span>
            {{ 'footer.healthy' | transloco }}
          </span>
        } @else {
          <span class="inline-flex items-center gap-1.5 rounded-full border border-red-500/20 bg-red-500/10 px-2 py-0.5 text-xs font-medium text-red-700 dark:text-red-400">
            <span class="h-1.5 w-1.5 rounded-full bg-red-500"></span>
            {{ 'footer.unhealthy' | transloco }}
          </span>
        }
      </div>
    </footer>
  `
})
export class FooterComponent implements OnInit {
  protected healthService = inject(HealthService);
  protected currentYear = new Date().getFullYear();

  ngOnInit(): void {
    this.healthService.startMonitoring(30000);
  }
}
