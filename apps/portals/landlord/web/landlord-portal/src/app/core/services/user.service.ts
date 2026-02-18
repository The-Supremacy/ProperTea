import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, catchError } from 'rxjs';

export interface UserDetails {
  userId: string;
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
  private userCache = signal(new Map<string, UserDetails>());

  getUserDetails(userId: string): Observable<UserDetails | null> {
    const cached = this.userCache().get(userId);
    if (cached) {
      return of(cached);
    }

    return this.http.get<UserDetails>(`/api/users/${userId}`).pipe(
      tap(userDetails => {
        this.userCache.update(cache => {
          const newCache = new Map(cache);
          newCache.set(userId, userDetails);
          return newCache;
        });
      }),
      catchError(() => {
        const fallback: UserDetails = {
          userId: userId,
          email: userId,
          displayName: userId.substring(0, 8)
        };
        this.userCache.update(cache => {
          const newCache = new Map(cache);
          newCache.set(userId, fallback);
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
