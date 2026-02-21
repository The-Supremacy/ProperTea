import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { UserPreferencesService } from '../../services/user-preferences.service';
import { HlmButton } from '@spartan-ng/helm/button';

@Component({
  selector: 'app-language-selector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton],
  template: `
    <button
      hlmBtn
      variant="ghost"
      size="icon"
      [class.ring-2]="currentLang() === 'en'"
      [class.ring-primary]="currentLang() === 'en'"
      (click)="setLanguage('en')"
      aria-label="English"
      [attr.aria-pressed]="currentLang() === 'en'">
      <span aria-hidden="true">🇺🇸</span>
    </button>
    <button
      hlmBtn
      variant="ghost"
      size="icon"
      [class.ring-2]="currentLang() === 'uk'"
      [class.ring-primary]="currentLang() === 'uk'"
      (click)="setLanguage('uk')"
      aria-label="Українська"
      [attr.aria-pressed]="currentLang() === 'uk'">
      <span aria-hidden="true">🇺🇦</span>
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
