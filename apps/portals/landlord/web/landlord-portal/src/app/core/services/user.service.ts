import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, catchError } from 'rxjs';

export interface UserDetails {
  internalId?: string; // GUID from internal user profile
  externalId: string;  // External user ID from Zitadel (used in tokens/audit logs)
  email: string;
  firstName?: string;
  lastName?: string;
  displayName: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  // Cache keyed by external user ID (from tokens/audit logs)
  private userCache = signal(new Map<string, UserDetails>());

  getUserDetails(externalUserId: string): Observable<UserDetails | null> {
    // Check cache first (cached by external user ID)
    const cached = this.userCache().get(externalUserId);
    if (cached) {
      return of(cached);
    }

    // Fetch from API and cache
    return this.http.get<UserDetails>(`/api/users/external/${externalUserId}`).pipe(
      tap(userDetails => {
        this.userCache.update(cache => {
          const newCache = new Map(cache);
          newCache.set(externalUserId, userDetails);
          return newCache;
        });
      }),
      catchError(() => {
        // Return a fallback if user not found
        const fallback: UserDetails = {
          externalId: externalUserId,
          email: externalUserId,
          displayName: externalUserId.substring(0, 8) // Show shortened ID
        };
        this.userCache.update(cache => {
          const newCache = new Map(cache);
          newCache.set(externalUserId, fallback);
          return newCache;
        });
        return of(fallback);
      })
    );
  }

  clearCache(): void {
    this.userCache.set(new Map());
  }
}
