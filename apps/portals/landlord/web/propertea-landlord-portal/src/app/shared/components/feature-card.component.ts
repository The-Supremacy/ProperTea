import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Feature card component for displaying key features with icon, title, and description
 * Used on landing and documentation pages
 */
@Component({
  selector: 'app-feature-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <article class="bg-surface-card border border-surface rounded-xl p-5 sm:p-6 shadow-md hover:shadow-lg transition-shadow">
      <div
        class="w-12 h-12 rounded-lg flex items-center justify-center mb-4 shrink-0"
        [ngClass]="iconBgClass()"
        role="img"
        [attr.aria-label]="iconLabel()">
        <i
          class="pi text-2xl"
          [ngClass]="[iconClass(), iconColorClass()]"
          aria-hidden="true">
        </i>
      </div>
      <h3 class="text-base sm:text-lg font-semibold mb-2 text-surface-900 dark:text-surface-0">
        <ng-content select="[title]"></ng-content>
      </h3>
      <p class="text-sm text-surface-600 dark:text-surface-400">
        <ng-content></ng-content>
      </p>
    </article>
  `
})
export class FeatureCardComponent {
  iconClass = input.required<string>(); // e.g., 'pi-building'
  iconLabel = input.required<string>(); // e.g., 'Properties icon'
  iconBgClass = input<string>('bg-primary/10');
  iconColorClass = input<string>('text-primary');
}
