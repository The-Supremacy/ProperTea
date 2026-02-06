import { Component, output, input, inject, viewChild, signal } from '@angular/core';
import { Menu, MenuContent, MenuItem as NgAriaMenuItem, MenuTrigger } from '@angular/aria/menu';
import { OverlayModule } from '@angular/cdk/overlay';
import { TranslocoPipe } from '@jsverse/transloco';
import { ResponsiveService } from '../../core/services/responsive.service';
import { UserPreferencesService } from '../../core/services/user-preferences.service';
import { ButtonDirective } from '../../../shared/components/button';
import { IconComponent } from '../../../shared/components/icon';
import { LogoComponent } from '../logo';

export interface LanguageOption {
  code: string;
  name: string;
  flag: string;
}

@Component({
  selector: 'app-header',
  imports: [Menu, MenuContent, NgAriaMenuItem, MenuTrigger, OverlayModule, ButtonDirective, IconComponent, TranslocoPipe, LogoComponent],

  template: `
    <header class="flex h-16 items-center justify-between border-b bg-background px-4">
      <!-- Left: Logo + Burger (mobile) -->
      <div class="flex items-center gap-4">
        @if (responsive.isMobile()) {
          <button
            appBtn
            variant="ghost"
            size="icon"
            (click)="menuToggle.emit()">
            <app-icon name="menu" [size]="20" />
          </button>
        }

        <button
          appBtn
          variant="ghost"
          (click)="logoClick.emit()">
          <app-logo [showText]="!responsive.isMobile()" />
        </button>
      </div>

      <!-- Center: Search (placeholder) -->
      <div class="flex-1 mx-4 max-w-md">
        @if (!responsive.isMobile()) {
          <input
            type="search"
            [placeholder]="'common.search' | transloco"
            class="input w-full"
            disabled />
        }
      </div>

      <!-- Right: User Menu -->
      <button
        appBtn
        variant="ghost"
        size="icon"
        ngMenuTrigger
        #trigger="ngMenuTrigger"
        #origin
        [menu]="userMenu()"
        class="rounded-full bg-muted hover:bg-accent">
        <app-icon name="account_circle" [size]="20" />
      </button>

      <ng-template
        [cdkConnectedOverlayOpen]="trigger.expanded()"
        [cdkConnectedOverlay]="{origin, usePopover: 'inline'}"
        [cdkConnectedOverlayPositions]="[
          {originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 4}
        ]"
        cdkAttachPopoverAsChild>

        <div
          ngMenu
          #userMenu="ngMenu"
          class="min-w-56 rounded-md border bg-popover p-1 text-popover-foreground shadow-md">
          <ng-template ngMenuContent>
            <!-- My Account Label -->
            <div class="px-2 py-1.5 text-sm font-semibold text-foreground">{{ 'user.profile' | transloco }}</div>
            <div role="separator" aria-orientation="horizontal" class="my-1 h-px bg-border"></div>

            <!-- Profile -->
            <div
              ngMenuItem
              value="profile"
              (click)="profileClick.emit()"
              class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent focus:bg-accent">
              <app-icon name="person" [size]="16" />
              <span>{{ 'user.profile' | transloco }}</span>
            </div>

            <!-- Preferences -->
            <div
              ngMenuItem
              value="preferences"
              (click)="preferencesClick.emit()"
              class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent focus:bg-accent">
              <app-icon name="settings" [size]="16" />
              <span>{{ 'user.preferences' | transloco }}</span>
            </div>

            <!-- Switch Account -->
            <div
              ngMenuItem
              value="switch-account"
              (click)="switchAccountClick.emit()"
              class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent focus:bg-accent">
              <app-icon name="business" [size]="16" />
              <span>{{ 'user.switchAccount' | transloco }}</span>
            </div>

            <div role="separator" aria-orientation="horizontal" class="my-1 h-px bg-border"></div>

            <!-- Sign Out -->
            <div
              ngMenuItem
              value="sign-out"
              (click)="signOutClick.emit()"
              class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent focus:bg-accent">
              <app-icon name="logout" [size]="16" />
              <span>{{ 'user.signOut' | transloco }}</span>
            </div>

            <div role="separator" aria-orientation="horizontal" class="my-1 h-px bg-border"></div>

            <!-- Theme Toggle -->
            <div
              ngMenuItem
              value="theme"
              (click)="preferencesService.toggleTheme()"
              class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent focus:bg-accent">
              @if (preferencesService.theme() === 'dark') {
                <app-icon name="light_mode" [size]="16" />
                <span>{{ 'user.lightMode' | transloco }}</span>
              } @else {
                <app-icon name="dark_mode" [size]="16" />
                <span>{{ 'user.darkMode' | transloco }}</span>
              }
            </div>

            <div role="separator" aria-orientation="horizontal" class="my-1 h-px bg-border"></div>

            <!-- Language Selector (Flags) -->
            <div class="flex flex-wrap gap-2 px-2 py-1.5">
              @for (lang of languages(); track lang.code) {
                <button
                  ngMenuItem
                  [value]="lang.code"
                  (click)="languageChange.emit(lang.code)"
                  [class]="'flex h-8 w-8 items-center justify-center rounded text-lg transition-colors hover:bg-accent ' +
                    (lang.code === currentLanguage().code ? 'ring-2 ring-primary' : '')"
                  [title]="lang.name">
                  {{ lang.flag }}
                </button>
              }
            </div>
          </ng-template>
        </div>
      </ng-template>
    </header>
  `
})
export class HeaderComponent {
  protected readonly responsive = inject(ResponsiveService);
  protected readonly preferencesService = inject(UserPreferencesService);

  languages = input.required<LanguageOption[]>();
  currentLanguage = input.required<LanguageOption>();

  menuToggle = output<void>();
  logoClick = output<void>();
  profileClick = output<void>();
  preferencesClick = output<void>();
  switchAccountClick = output<void>();
  languageChange = output<string>();
  signOutClick = output<void>();

  // ViewChild for menu reference
  protected userMenu = viewChild<Menu<string>>('userMenu');
}
