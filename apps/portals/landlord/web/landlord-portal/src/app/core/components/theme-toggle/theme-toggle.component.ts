import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { UserPreferencesService } from '../../services/user-preferences.service';
import { HlmButton } from '@spartan-ng/helm/button';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-theme-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, TranslocoPipe],
  template: `
    <button
      hlmBtn
      variant="ghost"
      size="icon"
      (click)="toggleTheme()"
      [attr.aria-label]="(isDarkMode() ? 'user.lightMode' : 'user.darkMode') | transloco">
      <span aria-hidden="true">{{ isDarkMode() ? '☀️' : '🌙' }}</span>
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
