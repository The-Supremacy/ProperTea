import { Component, computed, inject, output } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AvatarModule } from 'primeng/avatar';
import { InputTextModule } from 'primeng/inputtext';
import { AuthService } from '../../auth/services/auth.service';
import { SearchService } from '../../core/services/search.service';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LanguageSwitcherComponent } from '../../i18n/components/language-switcher.component';

@Component({
  selector: 'app-header',
  imports: [ButtonModule, MenuModule, AvatarModule, InputTextModule, TranslocoModule, LanguageSwitcherComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly searchService = inject(SearchService);
  private readonly translocoService = inject(TranslocoService);

  menuToggle = output<void>();
  searchQuery = this.searchService.searchQuery;

  userMenuItems = computed<MenuItem[]>(() => [
    { separator: true },
    {
      label: this.translocoService.translate('header.menu.editProfile'),
      icon: 'pi pi-user',
      command: () => this.editProfile()
    },
    {
      label: this.translocoService.translate('header.menu.editPreferences'),
      icon: 'pi pi-cog',
      command: () => this.editPreferences()
    },
    { separator: true },
    {
      label: this.translocoService.translate('header.menu.switchAccount'),
      icon: 'pi pi-refresh',
      command: () => this.switchAccount()
    },
    { separator: true },
    {
      label: this.translocoService.translate('header.menu.signOut'),
      icon: 'pi pi-sign-out',
      styleClass: 'text-red-500',
      command: () => this.signOut()
    }
  ]);

  userName = computed(() => this.authService.userName());
  userEmail = computed(() => this.authService.userEmail());
  organizationName = computed(() => this.authService.organizationName());
  userInitials = computed(() => this.authService.userInitials());

  onMenuToggle(): void {
    this.menuToggle.emit();
  }

  onSearchChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchService.setQuery(value);
  }

  editProfile(): void {
    this.authService.editProfile();
  }

  editPreferences(): void {
    this.router.navigate(['/preferences']);
  }

  switchAccount(): void {
    this.authService.select_account();
  }

  signOut(): void {
    const landingUrl = window.location.origin + '/';
    this.authService.logout(landingUrl);
  }
}
