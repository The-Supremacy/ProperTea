import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../../utils/cn';

const inputVariants = cva(
  // Base styles - always applied
  [
    'flex',
    'w-full',
    'rounded-md',
    'border',
    'bg-background',
    'px-3',
    'py-2',
    'text-sm',
    'ring-offset-background',
    'transition-colors',
    'placeholder:text-muted-foreground',
    'focus-visible:outline-none',
    'focus-visible:ring-2',
    'focus-visible:ring-ring',
    'focus-visible:ring-offset-2',
    'disabled:cursor-not-allowed',
    'disabled:opacity-50',
  ],
  {
    variants: {
      variant: {
        default: 'border-input',
        error: 'border-destructive focus-visible:ring-destructive',
        success: 'border-green-500 focus-visible:ring-green-500',
      },
      inputSize: {
        sm: 'h-8 text-xs px-2',
        md: 'h-10', // Default
        lg: 'h-12 text-base px-4',
      },
    },
    defaultVariants: {
      variant: 'default',
      inputSize: 'md',
    },
  }
);

export type InputVariants = VariantProps<typeof inputVariants>;

/**
 * Headless text input component with CVA-based variant styling.
 * Integrates with Angular forms and provides visual feedback for validation states.
 *
 * @example
 * ```html
 * <app-text-input
 *   id="name"
 *   type="text"
 *   [variant]="nameControl.invalid && nameControl.touched ? 'error' : 'default'"
 *   placeholder="Enter name"
 *   formControlName="name" />
 * ```
 */
@Component({
  selector: 'app-text-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[attr.type]': 'type()',
    '[attr.id]': 'id()',
    '[attr.placeholder]': 'placeholder()',
    '[attr.disabled]': 'disabled() ? "" : null',
    '[attr.required]': 'required() ? "" : null',
    '[attr.readonly]': 'readonly() ? "" : null',
    '[attr.autocomplete]': 'autocomplete()',
    '[attr.aria-invalid]': 'variant() === "error"',
    '[class]': 'computedClass()',
  },
  template: '',
})
export class TextInputComponent {
  // Inputs
  type = input<'text' | 'email' | 'password' | 'tel' | 'url' | 'search'>('text');
  id = input<string>();
  placeholder = input<string>('');
  disabled = input<boolean>(false);
  required = input<boolean>(false);
  readonly = input<boolean>(false);
  autocomplete = input<string>();

  // CVA variant inputs
  variant = input<InputVariants['variant']>('default');
  inputSize = input<InputVariants['inputSize']>('md');

  // Allow custom classes to be added
  class = input<string>('');

  // Computed class using CVA
  protected computedClass = computed(() =>
    cn(
      inputVariants({
        variant: this.variant(),
        inputSize: this.inputSize(),
      }),
      this.class()
    )
  );
}
