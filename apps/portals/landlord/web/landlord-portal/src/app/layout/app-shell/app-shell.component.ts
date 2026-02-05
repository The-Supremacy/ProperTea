import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { HeaderComponent, LanguageOption } from '../header/header.component';
import { NavigationComponent, MenuItem } from '../navigation/navigation.component';
import { FooterComponent } from '../footer/footer.component';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { SessionService } from '../../core/services/session.service';
import { HealthService } from '../../core/services/health.service';
import { UserPreferencesService } from '../../core/services/user-preferences.service';
import { ResponsiveService } from '../../core/services/responsive.service';

const AVAILABLE_LANGUAGES: LanguageOption[] = [
  { code: 'en', name: 'English', flag: '🇬🇧' },
  { code: 'uk', name: 'Українська', flag: '🇺🇦' }
];

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    HeaderComponent,
    NavigationComponent,
    FooterComponent,
    BreadcrumbComponent
  ],
  template: `
    <div class="flex h-screen flex-col overflow-hidden bg-background text-foreground">
      <app-header
        [languages]="languages"
        [currentLanguage]="getCurrentLanguage()"
        (menuToggle)="toggleMobileDrawer()"
        (logoClick)="navigateHome()"
        (profileClick)="openProfile()"
        (preferencesClick)="openPreferences()"
        (switchAccountClick)="switchAccount()"
        (languageChange)="changeLanguage($event)"
        (signOutClick)="signOut()" />

      <div class="flex flex-1 overflow-hidden">
        @if (!responsive.isMobile()) {
          <app-navigation
            [menuItems]="menuItems"
            [collapsed]="navCollapsed()"
            (toggleCollapse)="toggleNavCollapse()" />
        }

        @if (responsive.isMobile() && mobileDrawerOpen()) {
          <div
            class="fixed inset-0 z-50 bg-background/80 backdrop-blur-sm"
            (click)="closeMobileDrawer()">
            <div
              class="fixed inset-y-0 left-0 w-64 bg-background shadow-lg"
              (click)="$event.stopPropagation()">
              <app-navigation
                [menuItems]="menuItems"
                [collapsed]="false"
                [showLogo]="true" />
            </div>
          </div>
        }

        <main class="flex flex-1 flex-col overflow-hidden">
          <app-breadcrumb />
          <div class="flex-1 overflow-auto bg-muted/40 p-6">
            <router-outlet />
          </div>
        </main>
      </div>

      <app-footer />
    </div>
  `
})
export class AppShellComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private sessionService = inject(SessionService);
  private healthService = inject(HealthService);
  protected readonly preferencesService = inject(UserPreferencesService);
  protected readonly responsive = inject(ResponsiveService);

  private sessionSubscription?: Subscription;

  navCollapsed = signal(false);
  mobileDrawerOpen = signal(false);

  readonly languages: LanguageOption[] = AVAILABLE_LANGUAGES;

  menuItems: MenuItem[] = [
    {
      label: 'nav.dashboard',
      icon: 'dashboard',
      route: '/dashboard'
    },
    {
      label: 'nav.companies',
      icon: 'business',
      children: [
        { label: 'nav.companies', icon: 'list', route: '/companies' },
        { label: 'user.preferences', icon: 'settings', route: '/companies/settings' }
      ]
    }
  ];

  ngOnInit(): void {
    this.sessionSubscription = this.sessionService.loadSessionData().subscribe();
    this.healthService.startMonitoring();
    this.preferencesService.loadPreferences();
  }

  ngOnDestroy(): void {
    this.sessionSubscription?.unsubscribe();
  }

  protected toggleNavCollapse(): void {
    this.navCollapsed.update(v => !v);
  }

  protected toggleMobileDrawer(): void {
    this.mobileDrawerOpen.update(v => !v);
  }

  protected closeMobileDrawer(): void {
    this.mobileDrawerOpen.set(false);
  }

  protected navigateHome(): void {
    this.router.navigate(['/']);
  }

  protected openProfile(): void {
    const idpUrl = 'http://localhost:8080';
    window.open(`${idpUrl}/ui/console/users/me`, '_blank');
  }

  protected openPreferences(): void {
    this.router.navigate(['/preferences']);
  }

  protected switchAccount(): void {
    this.sessionService.switchAccount();
  }

  protected signOut(): void {
    this.sessionService.logout();
  }

  protected changeLanguage(code: string): void {
    this.preferencesService.setLanguage(code);
  }

  protected getCurrentLanguage(): LanguageOption {
    const currentCode = this.preferencesService.language();
    return this.languages.find(l => l.code === currentCode) ?? this.languages[0];
  }
}
