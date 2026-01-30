import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '@core';
import { UserPreferencesService } from './core/services/user-preferences.service';
import { LayoutComponent } from './layout/layout.component';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, LayoutComponent, ProgressSpinnerModule],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App implements OnInit {
  protected readonly authService = inject(AuthService);
  private readonly preferencesService = inject(UserPreferencesService);

  constructor() {
    this.initializeDarkMode();
  }

  async ngOnInit() {
    if (this.authService.isAuthenticated()) {
      await this.preferencesService.initialize();
    }

    await new Promise(resolve => setTimeout(resolve, 100));

    this.removeLoadingSpinner();
  }

  private removeLoadingSpinner() {
    const loader = document.getElementById('app-loading');
    if (loader) {
      loader.style.opacity = '0';
      loader.style.transition = 'opacity 0.3s ease-out';
      setTimeout(() => loader.remove(), 300);
    }
  }

  private initializeDarkMode() {
    const savedTheme = localStorage.getItem('theme');

    if (savedTheme === 'dark') {
      document.documentElement.classList.add('dark');
    } else if (savedTheme === 'light') {
      document.documentElement.classList.remove('dark');
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      if (prefersDark) {
        document.documentElement.classList.add('dark');
      }
    }

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      if (!localStorage.getItem('theme')) {
        if (e.matches) {
          document.documentElement.classList.add('dark');
        } else {
          document.documentElement.classList.remove('dark');
        }
      }
    });
  }
}
