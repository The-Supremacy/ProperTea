import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../../utils/cn';
import { IconComponent } from '../icon';

const errorVariants = cva(
  // Base styles
  ['flex', 'items-center', 'gap-1.5', 'text-sm', 'font-medium'],
  {
    variants: {
      variant: {
        error: 'text-destructive',
        warning: 'text-yellow-600 dark:text-yellow-500',
        info: 'text-blue-600 dark:text-blue-400',
        success: 'text-green-600 dark:text-green-500',
      },
      size: {
        sm: 'text-xs',
        md: 'text-sm',
        lg: 'text-base',
      },
    },
    defaultVariants: {
      variant: 'error',
      size: 'md',
    },
  }
);

export type ErrorVariants = VariantProps<typeof errorVariants>;

/**
 * Headless validation error component with CVA-based variant styling.
 * Displays validation messages with optional icons.
 *
 * @example
 * ```html
 * @if (nameControl.invalid && nameControl.touched) {
 *   <app-validation-error
 *     [message]="'companies.nameRequired' | transloco"
 *     [showIcon]="true" />
 * }
 *
 * @if (nameControl.pending) {
 *   <app-validation-error
 *     variant="info"
 *     [message]="'companies.checkingName' | transloco"
 *     icon="hourglass_empty"
 *     [showIcon]="true" />
 * }
 * ```
 */
@Component({
  selector: 'app-validation-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div [class]="computedClass()">
      @if (showIcon()) {
        <app-icon
          [name]="icon()"
          [size]="iconSize()"
          [class]="iconClass()" />
      }
      <span>{{ message() }}</span>
    </div>
  `,
})
export class ValidationErrorComponent {
  // Inputs
  message = input.required<string>();

  // CVA variant inputs
  variant = input<ErrorVariants['variant']>('error');
  size = input<ErrorVariants['size']>('md');

  // Icon configuration
  showIcon = input<boolean>(false);
  icon = input<string>('error');

  // Allow custom classes
  class = input<string>('');

  // Computed values
  protected computedClass = computed(() =>
    cn(
      errorVariants({
        variant: this.variant(),
        size: this.size(),
      }),
      this.class()
    )
  );

  protected iconSize = computed(() => {
    const sizeMap = {
      sm: 14,
      md: 16,
      lg: 18,
    };
    return sizeMap[this.size() || 'md'];
  });

  protected iconClass = computed(() => {
    if (this.showIcon() && this.icon() === 'hourglass_empty') {
      return 'animate-spin';
    }
    return '';
  });
}
