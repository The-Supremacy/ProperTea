import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { CommonModule } from '@angular/common';

interface Language {
  code: string;
  name: string;
  flag: string;
}

@Component({
  selector: 'app-language-switcher',
  imports: [CommonModule],
  templateUrl: './language-switcher.component.html',
  styleUrl: './language-switcher.component.scss'
})
export class LanguageSwitcherComponent {
  private readonly translocoService = inject(TranslocoService);

  languages: Language[] = [
    { code: 'en', name: 'English', flag: '🇬🇧' },
    { code: 'uk', name: 'Українська', flag: '🇺🇦' }
  ];

  isExpanded = signal(false);

  activeLang = toSignal(this.translocoService.langChanges$, {
    initialValue: this.translocoService.getActiveLang()
  });

  currentLanguage = computed(() =>
    this.languages.find(lang => lang.code === this.activeLang())
  );

  toggleExpanded(): void {
    this.isExpanded.update(value => !value);
  }

  selectLanguage(lang: string): void {
    this.translocoService.setActiveLang(lang);
    this.isExpanded.set(false);
  }
}
