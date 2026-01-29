import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router, CanActivateFn } from '@angular/router';
import { map, filter, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const authenticatedGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return toObservable(authService.isLoading).pipe(
    filter(loading => !loading),
    take(1),
    map(() => {
      if (!authService.isAuthenticated()) {
        return router.createUrlTree(['/']);
      }
      return true;
    })
  );
};
