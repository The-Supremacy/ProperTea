import { inject, Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { ProblemDetails, ValidationError } from '../models/error.models';

@Injectable({
  providedIn: 'root'
})
export class ErrorTranslatorService {
  private readonly translocoService = inject(TranslocoService);

  translateError(problemDetails: ProblemDetails): string {
    if (!problemDetails.errorCode) {
      return problemDetails.detail || problemDetails.title || 'An unexpected error occurred';
    }

    const translationKey = `errors.${problemDetails.errorCode}`;
    const translation = this.translocoService.translate(translationKey, problemDetails.parameters || {});

    if (translation === translationKey) {
      return problemDetails.detail || problemDetails.title || 'An unexpected error occurred';
    }

    return translation;
  }

  translateValidationErrors(problemDetails: ProblemDetails): ValidationError[] {
    const errors = (problemDetails.parameters?.['errors'] as Array<{
      field: string;
      errorCode: string;
      message: string;
      attemptedValue: unknown;
    }>) || [];

    return errors.map(error => ({
      field: error.field,
      errorCode: error.errorCode || 'VALIDATION.ERROR',
      parameters: error.attemptedValue ? { value: error.attemptedValue } : undefined
    }));
  }

  isValidationError(problemDetails: ProblemDetails): boolean {
    return problemDetails.status === 422;
  }

  translateErrorCode(errorCode: string, params?: Record<string, unknown>): string {
    return this.translocoService.translate(`errors.${errorCode}`, params || {});
  }
}
