import { Component, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-language-switcher',
  imports: [ButtonModule, CommonModule],
  template: `
    <div class="flex gap-2 align-items-center">
      <p-button
        [label]="'EN'"
        [outlined]="activeLang !== 'en'"
        [text]="activeLang === 'en'"
        size="small"
        (onClick)="switchLang('en')"
      />
      <p-button
        [label]="'UK'"
        [outlined]="activeLang !== 'uk'"
        [text]="activeLang === 'uk'"
        size="small"
        (onClick)="switchLang('uk')"
      />
    </div>
  `,
  styles: ``
})
export class LanguageSwitcherComponent {
  private readonly translocoService = inject(TranslocoService);

  get activeLang(): string {
    return this.translocoService.getActiveLang();
  }

  switchLang(lang: string): void {
    this.translocoService.setActiveLang(lang);
  }
}
