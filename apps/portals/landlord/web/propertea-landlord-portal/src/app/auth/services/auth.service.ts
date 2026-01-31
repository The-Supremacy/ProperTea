import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';
import { ConfigService } from '../../core/services/config.service';

export interface CurrentUser {
  isAuthenticated: boolean;
  emailAddress: string;
  firstName: string;
  lastName: string;
  organizationName: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly config = inject(ConfigService);
  private readonly http = inject(HttpClient);

  private readonly _currentUser = signal<CurrentUser>({
    isAuthenticated: false,
    emailAddress: '',
    firstName: '',
    lastName: '',
    organizationName: ''
  });
  private readonly _isLoading = signal<boolean>(true);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  readonly isAuthenticated = computed(() => this._currentUser().isAuthenticated);

  readonly userName = computed(() => {
    const user = this._currentUser();
    if (!user.firstName && !user.lastName)
      return 'User';
    return `${user.firstName} ${user.lastName}`.trim();
  });

  readonly userEmail = computed(() => this._currentUser().emailAddress);
  readonly organizationName = computed(() => this._currentUser().organizationName);
  readonly userInitials = computed(() => {
    const user = this._currentUser();
    if (user.firstName && user.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    return user.firstName ? user.firstName.substring(0, 2).toUpperCase() : 'U';
  });

  constructor() {
  }

  refresh$(): Observable<CurrentUser | null> {
    this._isLoading.set(true);

    return this.http.get<CurrentUser>('/auth/user').pipe(
      tap(user => this._currentUser.set(user)),
      catchError(() => {
        this._currentUser.set({
          isAuthenticated: false,
          emailAddress: '',
          firstName: '',
          lastName: '',
          organizationName: ''
        });
        return of(null);
      }),
      tap(() => this._isLoading.set(false))
    );
  }

  login(returnUrl?: string): void {
    const targetUrl = returnUrl !== undefined
      ? returnUrl
      : window.location.href;

    window.location.href = `/auth/login?returnUrl=${encodeURIComponent(targetUrl)}`;
  }

  logout(returnUrl?: string): void {
    const targetUrl = returnUrl !== undefined
      ? returnUrl
      : window.location.href;

    window.location.href = `/auth/logout?returnUrl=${encodeURIComponent(targetUrl)}`;
  }

  select_account(returnUrl?: string): void {
    const url = returnUrl || window.location.href;
    window.location.href = `/auth/select_account?returnUrl=${encodeURIComponent(url)}`;
  }

  editProfile(): void {
    window.open(`${this.config.idpUrl}/ui/console/users/me`, '_blank');
  }
}
