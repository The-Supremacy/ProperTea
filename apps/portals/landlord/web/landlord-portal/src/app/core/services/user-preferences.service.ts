import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';
import { catchError, of, tap } from 'rxjs';
import { SessionService } from './session.service';

export interface UserPreferences {
  theme: 'light' | 'dark';
  language: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserPreferencesService {
  private http = inject(HttpClient);
  private translocoService = inject(TranslocoService);
  private sessionService = inject(SessionService);
  private loaded = false;

  private preferences = signal<UserPreferences>({
    theme: this.getInitialTheme(),
    language: this.getInitialLanguage()
  });

  readonly theme = computed(() => this.preferences().theme);
  readonly language = computed(() => this.preferences().language);

  constructor() {
    effect(() => {
      const theme = this.preferences().theme;
      document.documentElement.classList.toggle('dark', theme === 'dark');
      localStorage.setItem('theme', theme);
    });

    effect(() => {
      const lang = this.preferences().language;
      this.translocoService.setActiveLang(lang);
      localStorage.setItem('language', lang);
    });
  }

  loadPreferences(): void {
    if (this.loaded)
      return;

    this.loaded = true;

    const localPreferences = this.preferences();

    this.http.get<UserPreferences>('/api/users/preferences')
      .pipe(
        catchError(() => {
          return of(localPreferences);
        }),
        tap(backendPrefs => {
          const merged: UserPreferences = {
            theme: localPreferences.theme !== this.getInitialTheme() ? localPreferences.theme : backendPrefs.theme,
            language: localPreferences.language !== this.getInitialLanguage() ? localPreferences.language : backendPrefs.language
          };

          this.preferences.set(merged);
        })
      )
      .subscribe();
  }

  setTheme(theme: 'light' | 'dark'): void {
    this.preferences.update(prefs => ({ ...prefs, theme }));
    this.savePreferences();
  }

  toggleTheme(): void {
    const newTheme = this.theme() === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }

  setLanguage(language: string): void {
    this.preferences.update(prefs => ({ ...prefs, language }));
    this.savePreferences();
  }

  private savePreferences(): void {
    if (!this.sessionService.isAuthenticated()) {
      return;
    }

    const prefs = this.preferences();
    this.http.put('/api/users/preferences', prefs)
      .pipe(catchError(() => of(null)))
      .subscribe();
  }

  private getInitialTheme(): 'light' | 'dark' {
    const stored = localStorage.getItem('theme');

    if (stored === 'light' || stored === 'dark') {
      return stored;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private getInitialLanguage(): string {
    return localStorage.getItem('language') ?? 'en';
  }
}
