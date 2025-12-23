import { Component, inject, signal, resource, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '@core';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, ButtonModule, DrawerModule, AvatarModule, TooltipModule],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  protected readonly authService = inject(AuthService);
  sidebarVisible = signal(true);

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
}
