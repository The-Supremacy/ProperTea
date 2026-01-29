import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Form field wrapper component with label and error handling
 * Provides consistent styling and accessibility for form inputs
 */
@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col gap-2">
      <label
        [for]="fieldId()"
        class="block text-sm sm:text-base font-semibold text-surface-900 dark:text-surface-0">
        <ng-content select="[label]"></ng-content>
        @if (required()) {
          <span class="text-red-500" aria-label="required">*</span>
        }
      </label>
      <ng-content select="[input]"></ng-content>
      @if (error()) {
        <small
          [id]="errorId()"
          class="block mt-2 text-sm text-red-500"
          role="alert">
          {{ error() }}
        </small>
      }
      @if (hint()) {
        <small
          class="block mt-2 text-sm text-surface-500"
          [attr.role]="hintLive() ? 'status' : null"
          [attr.aria-live]="hintLive() ? 'polite' : null">
          {{ hint() }}
        </small>
      }
    </div>
  `
})
export class FormFieldComponent {
  fieldId = input.required<string>();
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  hintLive = input<boolean>(false); // For dynamic hints like "checking availability"

  errorId = input.required<string>(); // e.g., 'orgName-error'
}
