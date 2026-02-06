import { Directive, input, computed, HostBinding, ElementRef, inject } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 cursor-pointer',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground shadow hover:bg-primary/90 border border-primary',
        destructive: 'bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90 border border-destructive',
        outline: 'border border-input bg-background shadow-sm hover:bg-accent hover:text-accent-foreground',
        secondary: 'bg-secondary text-secondary-foreground shadow-sm hover:bg-secondary/80 border border-secondary',
        ghost: 'hover:bg-accent hover:text-accent-foreground border border-transparent',
        link: 'text-primary underline-offset-4 hover:underline border-none',
      },
      size: {
        default: 'h-9 px-4 py-2',
        sm: 'h-8 rounded-md px-3 text-xs',
        lg: 'h-10 rounded-md px-8',
        icon: 'h-9 w-9',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

export type ButtonVariant = VariantProps<typeof buttonVariants>['variant'];
export type ButtonSize = VariantProps<typeof buttonVariants>['size'];

@Directive({
  selector: '[appBtn]',
  host: {
    '[class]': 'computedClasses()'
  }
})
export class ButtonDirective {
  variant = input<ButtonVariant>('default');
  size = input<ButtonSize>('default');

  protected computedClasses = computed(() => {
    return buttonVariants({
      variant: this.variant(),
      size: this.size()
    });
  });
}
