import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';

export interface SessionContext {
  isAuthenticated: boolean;
  emailAddress: string;
  firstName: string;
  lastName: string;
  organizationId: string;
  organizationName: string;
  userId: string;
}

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private http = inject(HttpClient);

  private sessionContext = signal<SessionContext | null>(null);
  private loadingState = signal(true);

  readonly context = this.sessionContext.asReadonly();
  readonly isLoading = this.loadingState.asReadonly();

  readonly isAuthenticated = computed(() => this.sessionContext()?.isAuthenticated ?? false);

  readonly userName = computed(() => {
    const user = this.sessionContext();

    if (!user) {
      return 'User';
    }

    if (!user.firstName && !user.lastName) {
      return 'User';
    }

    return `${user.firstName} ${user.lastName}`.trim();
  });

  readonly userInitials = computed(() => {
    const user = this.sessionContext();

    if (!user || !user.firstName) {
      return 'U';
    }

    if (user.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }

    return user.firstName.substring(0, 2).toUpperCase();
  });

  readonly userEmail = computed(() => this.sessionContext()?.emailAddress ?? '');
  readonly organizationName = computed(() => this.sessionContext()?.organizationName ?? '');

  refreshSessionData(): Observable<SessionContext | null> {
    this.loadingState.set(true);

    return this.http.get<SessionContext>('/api/session').pipe(
      tap(user => this.sessionContext.set(user)),
      catchError(() => {
        this.sessionContext.set({
          isAuthenticated: false,
          emailAddress: '',
          firstName: '',
          lastName: '',
          organizationName: '',
          organizationId: '',
          userId: ''
        });
        return of(null);
      }),
      tap(() => this.loadingState.set(false))
    );
  }

  login(): void {
    const targetUrl = window.location.href;
    window.location.href = `/auth/login?returnUrl=${encodeURIComponent(targetUrl)}`;
  }

  logout(): void {
    const targetUrl = window.location.origin;
    window.location.href = `/auth/logout?returnUrl=${encodeURIComponent(targetUrl)}`;
  }

  switchAccount(): void {
    const url = window.location.href;
    window.location.href = `/auth/select_account?returnUrl=${encodeURIComponent(url)}`;
  }
}
