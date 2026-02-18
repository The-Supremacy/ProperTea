import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { UserPreferencesService } from '../../services/user-preferences.service';
import { HlmButton } from '@spartan-ng/helm/button';

@Component({
  selector: 'app-theme-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton],
  template: `
    <button
      hlmBtn
      variant="ghost"
      size="icon"
      (click)="toggleTheme()"
      [title]="isDarkMode() ? 'Switch to light mode' : 'Switch to dark mode'">
      {{ isDarkMode() ? '☀️' : '🌙' }}
    </button>
  `
})
export class ThemeToggleComponent {
  private preferencesService = inject(UserPreferencesService);

  protected isDarkMode = () => this.preferencesService.theme() === 'dark';

  protected toggleTheme(): void {
    this.preferencesService.toggleTheme();
  }
}
