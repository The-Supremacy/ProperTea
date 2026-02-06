import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionService } from './core/services/session.service';
import { LoadingService } from './core/services/loading.service';
import { AppLayoutComponent } from './layout/layout.component';
import { Subscription, switchMap, tap } from 'rxjs';
import { UserPreferencesService } from './core/services/user-preferences.service';
import { SpinnerComponent } from '../shared/components/spinner/spinner.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppLayoutComponent, SpinnerComponent],
  template: `
    @if (sessionService.isAuthenticated()) {
      <app-layout />
    } @else {
      <router-outlet />
    }

    <!-- Global Loading Overlay -->
    @if (loadingService.isLoading()) {
      <div
        class="fixed inset-0 z-9999 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      >
        <app-spinner size="lg" />
      </div>
    }
  `,
})
export class App {
  sessionService = inject(SessionService);
  loadingService = inject(LoadingService);
  readonly preferencesService = inject(UserPreferencesService);
  private subscription?: Subscription;

  ngOnInit() {
    this.subscription = this.sessionService
      .refreshSessionData()
      .subscribe();
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }
}
