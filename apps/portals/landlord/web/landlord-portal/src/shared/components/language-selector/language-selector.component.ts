import { Component, inject } from '@angular/core';
import { UserPreferencesService } from '../../../app/core/services/user-preferences.service';
import { ButtonDirective } from '../button/button.directive';

@Component({
  selector: 'app-language-selector',
  imports: [ButtonDirective],
  standalone: true,
  template: `
    <button
      appBtn
      variant="ghost"
      size="icon"
      [class.ring-2]="currentLang() === 'en'"
      [class.ring-primary]="currentLang() === 'en'"
      (click)="setLanguage('en')"
      title="English">
      🇺🇸
    </button>
    <button
      appBtn
      variant="ghost"
      size="icon"
      [class.ring-2]="currentLang() === 'uk'"
      [class.ring-primary]="currentLang() === 'uk'"
      (click)="setLanguage('uk')"
      title="Українська">
      🇺🇦
    </button>
  `
})
export class LanguageSelectorComponent {
  private preferencesService = inject(UserPreferencesService);

  protected currentLang = () => this.preferencesService.language();

  protected setLanguage(lang: string): void {
    this.preferencesService.setLanguage(lang);
  }
}
