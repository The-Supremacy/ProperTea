import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        const status = err.status;

        if (status === 401 || status === 403) {
          return throwError(() => err);
        }

        const raw = err.error;
        let problem: ProblemDetails | null = null;
        if (raw && typeof raw === 'object') {
          problem = raw as ProblemDetails;
        } else if (typeof raw === 'string' && raw.length > 0) {
          try { problem = JSON.parse(raw) as ProblemDetails; } catch { /* ignore */ }
        }
        const message =
          problem?.detail ??
          problem?.title ??
          (status === 0 ? 'Network error — check your connection.' : `Unexpected error (${status}).`);

        toast.errorMessage(message);
      }

      return throwError(() => err);
    }),
  );
};
