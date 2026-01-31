import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { AuthService } from '@core';
import { UserPreferencesService } from './core/services/user-preferences.service';
import { LayoutComponent } from './layout/layout.component';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { Subscription } from 'rxjs';
import { tap, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, LayoutComponent, ProgressSpinnerModule],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App implements OnInit, OnDestroy {
  protected readonly authService = inject(AuthService);
  private readonly preferencesService = inject(UserPreferencesService);
  private readonly translocoService = inject(TranslocoService);
  private subscription?: Subscription;

  constructor() {
    this.initializePreferencesFromLocalStorage();
  }

  ngOnInit() {
    // All async initialization: auth + preferences backend sync
    this.subscription = this.authService.refresh$().pipe(
      switchMap(() => this.preferencesService.syncWithBackend$(
        this.authService.isAuthenticated()
      )),
      tap(() => this.removeLoadingSpinner())
    ).subscribe();
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  private removeLoadingSpinner() {
    const loader = document.getElementById('app-loading');
    if (loader) {
      loader.style.opacity = '0';
      loader.style.transition = 'opacity 0.3s ease-out';
      setTimeout(() => loader.remove(), 300);
    }
  }

  private initializePreferencesFromLocalStorage() {
    this.preferencesService.initializeFromLocalStorage();

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      const stored = localStorage.getItem('user_preferences');
      if (!stored) {
        document.documentElement.classList.toggle('dark', e.matches);
      }
    });
  }
}
