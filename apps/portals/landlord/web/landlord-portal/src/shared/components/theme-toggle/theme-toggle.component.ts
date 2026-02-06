import { Component, inject } from '@angular/core';
import { UserPreferencesService } from '../../../app/core/services/user-preferences.service';
import { ButtonDirective } from '../button/button.directive';

@Component({
  selector: 'app-theme-toggle',
  imports: [ButtonDirective],
  standalone: true,
  template: `
    <button
      appBtn
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
