import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';
import { catchError, of, tap } from 'rxjs';

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
    const localPreferences = this.preferences();

    this.http.get<UserPreferences>('/api/users/preferences')
      .pipe(
        catchError(() => {
          // If API fails, keep current local preferences
          return of(localPreferences);
        }),
        tap(backendPrefs => {
          // Merge backend preferences with local overrides
          // Local changes take precedence as they're the most recent
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
