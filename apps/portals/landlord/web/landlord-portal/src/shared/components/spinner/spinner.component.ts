import { Component, input, ChangeDetectionStrategy, computed } from '@angular/core';

@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="spinnerClasses()"
      role="status"
      aria-label="Loading">
      <svg
        class="animate-spin"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24">
        <circle
          class="opacity-25"
          cx="12"
          cy="12"
          r="10"
          stroke="currentColor"
          stroke-width="4">
        </circle>
        <path
          class="opacity-75"
          fill="currentColor"
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
        </path>
      </svg>
    </div>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `]
})
export class SpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');

  protected spinnerClasses = computed(() => {
    const sizeClasses = {
      sm: 'w-4 h-4',
      md: 'w-8 h-8',
      lg: 'w-12 h-12'
    };

    return `inline-block text-primary ${sizeClasses[this.size()]}`;
  });
}
