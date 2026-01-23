import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from '../../../core/services/config.service';

export interface UserInfo {
  isAuthenticated: boolean;
  claims: Array<{ type: string; value: string }>;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly config = inject(ConfigService);
  private readonly http = inject(HttpClient);

  private readonly _userInfo = signal<UserInfo>({ isAuthenticated: false, claims: [] });
  private readonly _isLoading = signal<boolean>(true);

  readonly userInfo = this._userInfo.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  readonly isAuthenticated = computed(() => this._userInfo().isAuthenticated);
  readonly claims = computed(() => this._userInfo().claims);

  readonly userName = computed(() => {
    const claims = this._userInfo().claims;
    const nameClaim = claims.find((c) => c.type === 'name' || c.type === 'preferred_username');
    return nameClaim?.value ?? 'User';
  });

  readonly userEmail = computed(() => {
    const claims = this._userInfo().claims;
    return claims.find((c) => c.type === 'email')?.value ?? '';
  });

  constructor() {
    this.refresh();
  }

  async refresh(): Promise<void> {
    this._isLoading.set(true);
    try {
      const user = await firstValueFrom(this.http.get<UserInfo>('/auth/user'));
      this._userInfo.set(user);
    } catch (error) {
      this._userInfo.set({ isAuthenticated: false, claims: [] });
    } finally {
      this._isLoading.set(false);
    }
  }

  getClaim(type: string): string | undefined {
    return this._userInfo().claims.find((c) => c.type === type)?.value;
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
    window.open(`${this.config.idpUrl}/ui/console/users/me`, '_blank');
  }

  register(returnUrl?: string): void {
    const url = returnUrl
      ? `/auth/register?returnUrl=${encodeURIComponent(returnUrl)}`
      : `/auth/register?returnUrl=${encodeURIComponent(window.location.href)}`;
    window.location.href = url;
  }
}
