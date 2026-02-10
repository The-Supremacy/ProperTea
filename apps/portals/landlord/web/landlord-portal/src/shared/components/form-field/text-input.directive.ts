import { Directive, input, computed } from '@angular/core';
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
 * Headless text input directive with CVA-based variant styling.
 * Applied to native input elements to provide consistent styling and visual feedback for validation states.
 *
 * @example
 * ```html
 * <input
 *   appTextInput
 *   id="name"
 *   type="text"
 *   [variant]="nameControl.invalid && nameControl.touched ? 'error' : 'default'"
 *   placeholder="Enter name"
 *   formControlName="name" />
 * ```
 */
@Directive({
  selector: 'input[appTextInput]',
  host: {
    '[class]': 'computedClass()',
  },
})
export class TextInputDirective {
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

