import { Component, DestroyRef, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { SessionService } from './core/services/session.service';
import { LoadingService } from './core/services/loading.service';
import { AppLayoutComponent } from './layout/layout.component';
import { UserPreferencesService } from './core/services/user-preferences.service';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmToaster } from '@spartan-ng/helm/sonner';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AppLayoutComponent, HlmSpinner, HlmToaster, TranslocoPipe],
  template: `
    @if (sessionService.isAuthenticated()) {
      <app-layout />
    } @else {
      <router-outlet />
    }

    <!-- Global Loading Overlay -->
    @if (loadingService.isLoading()) {
      <div
        role="status"
        aria-live="assertive"
        class="fixed inset-0 z-9999 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      >
        <hlm-spinner size="lg" />
        <span class="sr-only">{{ 'common.loading' | transloco }}</span>
      </div>
    }

    <hlm-toaster richColors position="bottom-right" />
  `,
})
export class App implements OnInit {
  protected sessionService = inject(SessionService);
  protected loadingService = inject(LoadingService);
  protected readonly preferencesService = inject(UserPreferencesService);
  private destroyRef = inject(DestroyRef);

  ngOnInit() {
    this.sessionService
      .refreshSessionData()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }
}
