import { Component, inject, signal, resource, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '@core';
import { HeaderComponent } from './header/header.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, ButtonModule, DrawerModule, HeaderComponent, TranslocoModule],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  protected readonly authService = inject(AuthService);
  mobileMenuVisible = signal(false);
  currentYear = new Date().getFullYear();

  readonly navigationItems = [
    { route: '/dashboard', icon: 'pi pi-home', labelKey: 'nav.dashboard' },
    { route: '/properties', icon: 'pi pi-building', labelKey: 'nav.properties' },
    { route: '/tenants', icon: 'pi pi-users', labelKey: 'nav.tenants' }
  ];

  readonly systemHealth = resource({
    loader: async () => {
      try {
        const res = await fetch('/health');
        return res.ok ? 'Healthy' : 'Unhealthy';
      } catch {
        return 'Offline';
      }
    }
  });

  readonly healthStatus = computed(() => this.systemHealth.value() ?? 'Checking...');

  toggleMenu(): void {
    this.mobileMenuVisible.update(v => !v);
  }
}
