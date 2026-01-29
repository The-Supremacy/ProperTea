import { Component, computed, input } from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ValidationError } from '../../../core/models/error.models';
import { inject } from '@angular/core';

@Component({
  selector: 'app-validation-errors',
  imports: [TranslocoModule],
  template: `
    @if (errors().length > 0) {
      <div
        class="border border-red-300 bg-red-50 dark:bg-red-900/20 rounded p-3"
        role="alert"
        [attr.aria-label]="ariaLabel()"
      >
        <h3 class="text-red-700 dark:text-red-400 font-semibold mb-2 text-sm">
          {{ title() | transloco }}
        </h3>
        <ul class="list-disc list-inside space-y-1">
          @for (error of translatedErrors(); track error.field) {
            <li class="text-red-600 dark:text-red-400 text-sm">
              <strong>{{ error.field }}:</strong> {{ error.message }}
            </li>
          }
        </ul>
      </div>
    }
  `,
  styles: ``
})
export class ValidationErrorsComponent {
  private readonly translocoService = inject(TranslocoService);

  errors = input.required<ValidationError[]>();
  title = input<string>('register.error.validationErrors');
  ariaLabel = input<string>('Validation errors');
  translatedErrors = computed(() =>
    this.errors().map(error => ({
      field: error.field,
      message: this.translocoService.translate(`errors.${error.errorCode}`, error.parameters || {})
    }))
  );
}
