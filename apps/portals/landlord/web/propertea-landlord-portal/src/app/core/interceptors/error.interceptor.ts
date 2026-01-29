import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * HTTP interceptor for global error handling
 * Handles common HTTP errors and redirects/notifications
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle different error status codes
      switch (error.status) {
        case 401:
          // Unauthorized - redirect to login
          router.navigate(['/auth/login'], {
            queryParams: { returnUrl: router.url }
          });
          break;

        case 403:
          // Forbidden - show access denied message
          console.error('Access denied:', error.message);
          break;

        case 404:
          // Not found
          console.error('Resource not found:', error.message);
          break;

        case 500:
        case 502:
        case 503:
          // Server errors
          console.error('Server error:', error.message);
          break;

        default:
          // Other errors
          console.error('HTTP error:', error);
      }

      // Re-throw the error for component-level handling
      return throwError(() => error);
    })
  );
};
