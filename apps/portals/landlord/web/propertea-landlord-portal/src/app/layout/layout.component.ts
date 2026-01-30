import { Component, inject, signal, resource, computed, effect } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '@core';
import { UserPreferencesService } from '../core/services/user-preferences.service';
import { HeaderComponent } from './header/header.component';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    DrawerModule,
    PanelMenuModule,
    HeaderComponent,
    BreadcrumbComponent,
    TranslocoModule,
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translocoService = inject(TranslocoService);
  private readonly preferencesService = inject(UserPreferencesService);
  mobileMenuVisible = signal(false);
  currentYear = new Date().getFullYear();

  constructor() {
    effect(
      () => {
        if (this.authService.isAuthenticated()) {
          this.preferencesService.initialize();
        }
      },
      { allowSignalWrites: true },
    );
  }

  readonly navigationMenuItems = computed<MenuItem[]>(() => {
    this.preferencesService.getPreferences()();

    return [
      {
        label: this.translocoService.translate('nav.dashboard'),
        icon: 'pi pi-home',
        command: () => {
          this.router.navigate(['/dashboard']);
          this.mobileMenuVisible.set(false);
        }
      },
      {
        label: this.translocoService.translate('nav.properties'),
        icon: 'pi pi-building',
        items: [
          {
            label: this.translocoService.translate('nav.properties'),
            icon: 'pi pi-building',
            command: () => {
              this.router.navigate(['/properties']);
              this.mobileMenuVisible.set(false);
            }
          }
        ]
      },
      {
        label: this.translocoService.translate('nav.tenants'),
        icon: 'pi pi-users',
        command: () => {
          this.router.navigate(['/tenants']);
          this.mobileMenuVisible.set(false);
        }
      }
    ];
  });

  readonly systemHealth = resource({
    loader: async () => {
      try {
        const res = await fetch('/health');
        return res.ok ? 'Healthy' : 'Unhealthy';
      } catch {
        return 'Offline';
      }
    },
  });

  readonly healthStatus = computed(() => this.systemHealth.value() ?? 'Checking...');

  toggleMenu(): void {
    this.mobileMenuVisible.update((v) => !v);
  }
}
