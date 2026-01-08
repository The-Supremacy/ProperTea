import { Injectable, signal, computed, resource, ResourceRef, inject } from '@angular/core';
import { ConfigService } from './config.service';

export interface UserInfo {
  isAuthenticated: boolean;
  claims: Array<{ type: string; value: string }>;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly config = inject(ConfigService);
  private readonly refreshTrigger = signal(0);

  readonly userResource: ResourceRef<UserInfo | undefined> = resource({
    params: () => ({ refreshTrigger: this.refreshTrigger() }),
    loader: async () => {
      try {
        const response = await fetch('/auth/user', {
          credentials: 'include'
        });

        if (response.status === 401 || response.status === 403) {
          return { isAuthenticated: false, claims: [] };
        }

        if (!response.ok) {
          return { isAuthenticated: false, claims: [] };
        }

        return await response.json() as UserInfo;
      } catch {
        return { isAuthenticated: false, claims: [] };
      }
    }
  });

  readonly isAuthenticated = computed(() =>
    this.userResource.value()?.isAuthenticated ?? false
  );

  readonly isLoading = computed(() =>
    this.userResource.isLoading()
  );

  readonly claims = computed(() =>
    this.userResource.value()?.claims ?? []
  );

  readonly userName = computed(() => {
    const claims = this.userResource.value()?.claims ?? [];
    const nameClaim = claims.find(c =>
      c.type === 'name' || c.type === 'preferred_username'
    );
    return nameClaim?.value ?? 'User';
  });

  readonly userEmail = computed(() => {
    const claims = this.userResource.value()?.claims ?? [];
    const emailClaim = claims.find(c => c.type === 'email');
    return emailClaim?.value ?? '';
  });

  getClaim(type: string): string | undefined {
    const claims = this.userResource.value()?.claims ?? [];
    return claims.find(c => c.type === type)?.value;
  }

  login(returnUrl?: string): void {
    const url = returnUrl
      ? `/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
      : `/auth/login?returnUrl=${encodeURIComponent(window.location.href)}`;
    window.location.href = url;
  }

  logout(returnUrl?: string): void {
    const url = returnUrl
      ? `/auth/logout?returnUrl=${encodeURIComponent(returnUrl)}`
      : `/auth/logout?returnUrl=${encodeURIComponent(window.location.href)}`;
    window.location.href = url;
  }

  editProfile(): void {
    window.open(`${this.config.zitadelUrl}/ui/console/users/me`, '_blank');
  }

  register(returnUrl?: string): void {
    const url = returnUrl
      ? `/auth/register?returnUrl=${encodeURIComponent(returnUrl)}`
      : `/auth/register?returnUrl=${encodeURIComponent(window.location.href)}`;
    window.location.href = url;
  }

  refresh(): void {
    this.refreshTrigger.update(v => v + 1);
  }
}
