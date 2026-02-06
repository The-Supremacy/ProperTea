import { Component, input, output, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ResponsiveService } from '../../core/services/responsive.service';
import { IconComponent } from '../../../shared/components/icon';
import { LogoComponent } from '../../../shared/components/logo';

export interface MenuItem {
  label: string;
  icon: string;
  route?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-navigation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, IconComponent, TranslocoPipe, LogoComponent],

  template: `
    <nav
      class="flex flex-col h-full border-r bg-background transition-all duration-200"
      [class.w-64]="!collapsed()"
      [class.w-16]="collapsed()">

      <!-- Mobile Logo -->
      @if (responsive.isMobile() && showLogo()) {
        <div class="border-b p-4">
          <app-logo />
        </div>
      }

      <div class="flex flex-col gap-2 p-2 flex-1 overflow-y-auto">
        @for (item of menuItems(); track item.label) {
          @if (item.children) {
            <!-- Menu item with submenu -->
            <div class="flex flex-col">
              <button
                class="flex items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-accent"
                (click)="toggleSubmenu(item.label)">
                <app-icon [name]="item.icon" [size]="20" />
                @if (!collapsed()) {
                  <span class="flex-1 text-left">{{ item.label | transloco }}</span>
                  <app-icon name="expand_more" [size]="16"
                    [class.rotate-180]="expandedMenus().has(item.label)" />
                }
              </button>

              @if (!collapsed() && expandedMenus().has(item.label)) {
                <div class="ml-6 mt-1 flex flex-col gap-1">
                  @for (child of item.children; track child.label) {
                    <a
                      [routerLink]="child.route"
                      routerLinkActive="bg-accent"
                      class="flex items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-accent">
                      <app-icon [name]="child.icon" [size]="16" />
                      <span>{{ child.label | transloco }}</span>
                    </a>
                  }
                </div>
              }
            </div>
          } @else {
            <!-- Simple menu item -->
            <a
              [routerLink]="item.route"
              routerLinkActive="bg-accent"
              class="flex items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-accent">
              <app-icon [name]="item.icon" [size]="20" />
              @if (!collapsed()) {
                <span>{{ item.label | transloco }}</span>
              }
            </a>
          }
        }
      </div>

      <!-- Collapse Toggle (docked at bottom) -->
      @if (!responsive.isMobile()) {
        <div class="border-t p-2">
          <button
            class="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-accent"
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
    </nav>
  `
})
export class NavigationComponent {
  protected readonly responsive = inject(ResponsiveService);

  menuItems = input.required<MenuItem[]>();
  collapsed = input<boolean>(false);
  showLogo = input<boolean>(false);

  toggleCollapse = output<void>();

  expandedMenus = signal(new Set<string>());

  toggleSubmenu(label: string): void {
    this.expandedMenus.update(set => {
      const next = new Set(set);
      if (next.has(label)) {
        next.delete(label);
      } else {
        next.add(label);
      }
      return next;
    });
  }
}
