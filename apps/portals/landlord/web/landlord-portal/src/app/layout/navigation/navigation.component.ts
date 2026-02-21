import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ResponsiveService } from '../../core/services/responsive.service';
import { IconComponent } from '../../../shared/components/icon';
import { LogoComponent } from '../../../shared/components/logo';
import { HlmNavigationMenuImports } from '@spartan-ng/helm/navigation-menu';

export interface MenuItem {
  label: string;
  icon: string;
  route?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-navigation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe, IconComponent, LogoComponent, HlmNavigationMenuImports],

  template: `
    <div
      class="flex h-full flex-col border-r bg-background transition-all duration-200"
      [class.w-64]="!collapsed()"
      [class.w-16]="collapsed()">

      <!-- Mobile Logo -->
      @if (responsive.isMobile() && showLogo()) {
        <div class="border-b p-4">
          <app-logo />
        </div>
      }

      <nav hlmNavigationMenu orientation="vertical" class="flex-1 overflow-y-auto">
        <ul hlmNavigationMenuList>
          @for (item of menuItems(); track item.label) {
            <li hlmNavigationMenuItem>
              <a
                hlmNavigationMenuLink
                [routerLink]="item.route"
                [active]="isActiveRoute(item.route)">
                <app-icon [name]="item.icon" [size]="20" class="shrink-0" />
                @if (!collapsed()) {
                  <span class="truncate">{{ item.label | transloco }}</span>
                }
              </a>
            </li>
          }
        </ul>
      </nav>

      <!-- Collapse Toggle (docked at bottom) -->
      @if (!responsive.isMobile()) {
        <div class="border-t p-2">
          <button
            class="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-accent"
            [attr.aria-label]="'nav.collapse' | transloco"
            [attr.aria-expanded]="!collapsed()"
            (click)="toggleCollapse.emit()">
            @if (collapsed()) {
              <app-icon name="chevron_right" [size]="20" />
            } @else {
              <app-icon name="chevron_left" [size]="20" />
              <span>{{ 'nav.collapse' | transloco }}</span>
            }
          </button>
        </div>
      }
    </div>
  `
})
export class NavigationComponent {
  protected readonly responsive = inject(ResponsiveService);
  private readonly router = inject(Router);

  menuItems = input.required<MenuItem[]>();
  collapsed = input<boolean>(false);
  showLogo = input<boolean>(false);

  toggleCollapse = output<void>();

  isActiveRoute(route: string | undefined): boolean {
    if (!route) return false;
    return this.router.isActive(route, {
      paths: 'subset',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    });
  }
}
