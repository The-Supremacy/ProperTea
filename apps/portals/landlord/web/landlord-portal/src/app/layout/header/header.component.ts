import { Component, output, input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ResponsiveService } from '../../core/services/responsive.service';
import { UserPreferencesService } from '../../core/services/user-preferences.service';
import { SessionService } from '../../core/services/session.service';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { IconComponent } from '../../../shared/components/icon';
import { LogoComponent } from '../../../shared/components/logo';

export interface LanguageOption {
  code: string;
  name: string;
  flag: string;
}

@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmDropdownMenuImports, HlmButton, HlmInput, IconComponent, TranslocoPipe, LogoComponent],
  template: `
    <header class="flex h-16 items-center justify-between border-b bg-background px-4">
      <!-- Left: Logo + Burger (mobile) -->
      <div class="flex items-center gap-4">
        @if (responsive.isMobile()) {
          <button hlmBtn variant="ghost" size="icon" (click)="menuToggle.emit()">
            <app-icon name="menu" [size]="20" />
          </button>
        }

        <button hlmBtn variant="ghost" (click)="logoClick.emit()">
          <app-logo [showText]="!responsive.isMobile()" />
        </button>
      </div>

      <!-- Center: Search (placeholder) -->
      <div class="flex-1 mx-4 max-w-md">
        @if (!responsive.isMobile()) {
          <input
            type="search"
            [placeholder]="'common.search' | transloco"
            hlmInput
            class="w-full"
            disabled />
        }
      </div>

      <!-- Right: User Menu -->
      <button
        hlmBtn
        variant="ghost"
        size="icon"
        [hlmDropdownMenuTrigger]="userMenu"
        class="rounded-full bg-muted hover:bg-accent">
        <app-icon name="account_circle" [size]="20" />
      </button>

      <ng-template #userMenu>
        <div hlmDropdownMenu class="min-w-56">
          <!-- User Name Label -->
          <div class="px-2 py-1.5 text-sm font-semibold text-foreground">
            {{ sessionService.userName() }}
          </div>
          <div hlmDropdownMenuSeparator></div>

          <!-- Profile -->
          <button hlmDropdownMenuItem (click)="profileClick.emit()">
            <app-icon name="person" [size]="16" />
            <span>{{ 'user.profile' | transloco }}</span>
          </button>

          <!-- Preferences -->
          <button hlmDropdownMenuItem (click)="preferencesClick.emit()">
            <app-icon name="settings" [size]="16" />
            <span>{{ 'user.preferences' | transloco }}</span>
          </button>

          <!-- Switch Account -->
          <button hlmDropdownMenuItem (click)="switchAccountClick.emit()">
            <app-icon name="business" [size]="16" />
            <span>{{ 'user.switchAccount' | transloco }}</span>
          </button>

          <div hlmDropdownMenuSeparator></div>

          <!-- Sign Out -->
          <button hlmDropdownMenuItem (click)="signOutClick.emit()">
            <app-icon name="logout" [size]="16" />
            <span>{{ 'user.signOut' | transloco }}</span>
          </button>

          <div hlmDropdownMenuSeparator></div>

          <!-- Theme Toggle -->
          <button hlmDropdownMenuItem (click)="preferencesService.toggleTheme()">
            @if (preferencesService.theme() === 'dark') {
              <app-icon name="light_mode" [size]="16" />
              <span>{{ 'user.lightMode' | transloco }}</span>
            } @else {
              <app-icon name="dark_mode" [size]="16" />
              <span>{{ 'user.darkMode' | transloco }}</span>
            }
          </button>

          <div hlmDropdownMenuSeparator></div>

          <!-- Language Selector (Flags) -->
          <div class="flex flex-wrap gap-2 px-2 py-1.5">
            @for (lang of languages(); track lang.code) {
              <button
                hlmDropdownMenuItem
                (click)="languageChange.emit(lang.code)"
                [class]="'flex h-8 w-8 items-center justify-center rounded text-lg transition-colors hover:bg-accent ' +
                  (lang.code === currentLanguage().code ? 'ring-2 ring-primary' : '')"
                [title]="lang.name">
                {{ lang.flag }}
              </button>
            }
          </div>
        </div>
      </ng-template>
    </header>
  `,
})
export class HeaderComponent {
  protected readonly responsive = inject(ResponsiveService);
  protected readonly preferencesService = inject(UserPreferencesService);
  protected readonly sessionService = inject(SessionService);

  languages = input.required<LanguageOption[]>();
  currentLanguage = input.required<LanguageOption>();

  menuToggle = output<void>();
  logoClick = output<void>();
  profileClick = output<void>();
  preferencesClick = output<void>();
  switchAccountClick = output<void>();
  languageChange = output<string>();
  signOutClick = output<void>();
}
