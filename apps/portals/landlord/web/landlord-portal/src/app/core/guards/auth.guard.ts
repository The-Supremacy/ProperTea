import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { map } from 'rxjs/operators';
import { SessionService } from '../services/session.service';

export const authGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);

  if (sessionService.isLoading()) {
    return sessionService.refreshSessionData().pipe(
      map(session => {
        if (session?.isAuthenticated) {
          return true;
        }

        sessionService.login();
        return false;
      })
    );
  }

  if (sessionService.isAuthenticated()) {
    return true;
  }

  sessionService.login();
  return false;
};
