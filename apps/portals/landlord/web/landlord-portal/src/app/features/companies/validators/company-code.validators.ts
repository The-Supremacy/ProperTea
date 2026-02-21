import { inject } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { CompanyService } from '../services/company.service';

/**
 * Async validator that checks if a company code is unique.
 * Debounces requests by 400ms to avoid excessive API calls.
 *
 * @param excludeId - Optional company ID to exclude from uniqueness check (for edit scenarios)
 * @returns AsyncValidatorFn that returns null if valid or { codeTaken: true } if invalid
 */
export function uniqueCompanyCode(excludeId?: string): AsyncValidatorFn {
  const companyService = inject(CompanyService);

  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    if (!control.value || control.value.trim() === '' || control.pristine) {
      return of(null);
    }

    return timer(400).pipe(
      switchMap(() =>
        companyService.checkCode(control.value.trim(), excludeId).pipe(
          map((response) => (response.available ? null : { codeTaken: true })),
          catchError(() => of(null)) // On error, don't block the form
        )
      )
    );
  };
}
