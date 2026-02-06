import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { filter, map, take } from 'rxjs/operators';
import { toObservable } from '@angular/core/rxjs-interop';
import { SessionService } from '../services/session.service';

export const authGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  if (!sessionService.isLoading() && sessionService.isAuthenticated()) {
    return true;
  }

  if (!sessionService.isLoading() && !sessionService.isAuthenticated()) {
    sessionService.login();
    return false;
  }

  return toObservable(sessionService.isLoading).pipe(
    filter(loading => !loading),
    take(1),
    map(() => {
      if (sessionService.isAuthenticated()) {
        return true;
      }
      sessionService.login();
      return false;
    })
  );
};
