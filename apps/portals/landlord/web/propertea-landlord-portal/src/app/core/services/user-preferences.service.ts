import { Injectable, inject, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom, catchError, of } from 'rxjs';
import { UserPreferences, UpdateUserPreferencesRequest } from '../models/user-preferences.model';

@Injectable({
  providedIn: 'root'
})
export class UserPreferencesService {
  private readonly http = inject(HttpClient);
  private readonly transloco = inject(TranslocoService);

  private readonly preferences = signal<UserPreferences>({
    theme: 'light',
    language: 'en'
  });

  private isInitialized = false;

  constructor() {
    effect(() => {
      const theme = this.preferences().theme;
      document.documentElement.classList.toggle('dark', theme === 'dark');
    });
  }

  async initialize(): Promise<void> {
    if (this.isInitialized)
      return;

    const localPrefs = this.loadFromLocalStorage();

    try {
      const backendPrefs = await this.fetchFromBackend();

      const merged: UserPreferences = {
        theme: backendPrefs?.theme ?? localPrefs.theme,
        language: backendPrefs?.language ?? localPrefs.language
      };

      this.preferences.set(merged);
      this.saveToLocalStorage(merged);
      this.transloco.setActiveLang(merged.language);

      const needsSync = JSON.stringify(localPrefs) !== JSON.stringify(merged);

      if (needsSync) {
        this.syncToBackend(merged);
      }
    } catch (error) {
      console.warn('Failed to fetch preferences from backend, using local only', error);
      this.preferences.set(localPrefs);
      this.transloco.setActiveLang(localPrefs.language);
    }

    this.isInitialized = true;
  }

  getPreferences() {
    return this.preferences.asReadonly();
  }

  setTheme(theme: 'light' | 'dark'): void {
    this.preferences.update(p => ({ ...p, theme }));
    this.saveToLocalStorage(this.preferences());

    this.syncToBackend(this.preferences());
  }

  setLanguage(language: string): void {
    this.preferences.update(p => ({ ...p, language }));
    this.transloco.setActiveLang(language);
    this.saveToLocalStorage(this.preferences());

    this.syncToBackend(this.preferences());
  }

  private loadFromLocalStorage(): UserPreferences {
    const stored = localStorage.getItem('user_preferences');
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch {
        // Invalid JSON, use defaults
      }
    }

    return {
      theme: 'light',
      language: this.transloco.getActiveLang()
    };
  }

  private saveToLocalStorage(prefs: UserPreferences): void {
    localStorage.setItem('user_preferences', JSON.stringify(prefs));
  }

  private async fetchFromBackend(): Promise<UserPreferences | null> {
    return firstValueFrom(
      this.http.get<UserPreferences>('/api/users/preferences').pipe(
        catchError(() => of(null))
      )
    );
  }

  private syncToBackend(prefs: UserPreferences): void {
    const request: UpdateUserPreferencesRequest = {
      theme: prefs.theme,
      language: prefs.language
    };

    this.http.put('/api/users/preferences', request)
      .subscribe({
        error: (err) => console.warn('Failed to sync preferences to backend', err)
      });
  }
}

